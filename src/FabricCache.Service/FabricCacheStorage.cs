using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using MS.Extensions.Caching.ServiceFabric.Model;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal class FabricCacheStorage : ICacheStorage
    {
        private const string DictNamesKey = "cache";
        private readonly IReliableStateManager _stateManager;
        private readonly ILogger<FabricCacheStorage> _logger;

        public FabricCacheStorage(IReliableStateManager stateManager, ILogger<FabricCacheStorage> logger)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger;
        }

        public async Task<byte[]> GetCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var metaDictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, CacheEntry>>(GetMetadataDictionaryName(clientId, tenantId));
            if (!metaDictionary.HasValue) return null;
            var dataDictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, byte[]>>(GetDataDictionaryName(clientId, tenantId));
            if (!dataDictionary.HasValue) return null;
            ConditionalValue<CacheEntry> meta;
            var data = new ConditionalValue<byte[]>();
            bool isExpired;
            using (var transaction = _stateManager.CreateTransaction())
            {
                meta = await metaDictionary.Value.TryGetValueAsync(transaction, key, LockMode.Default, timeout, cancellationToken);
                if (!meta.HasValue) return null;
                isExpired = IsExpired(meta.Value);
                if (!isExpired)
                {
                    data = await dataDictionary.Value.TryGetValueAsync(transaction, key, LockMode.Default, timeout, cancellationToken);
                    if (!data.HasValue) return null;
                }
            }

            if (isExpired)
            {
                await DeleteCacheItemAsync(key, clientId, tenantId, timeout, cancellationToken);
                return null;
            }
            else
            {
                _ = await UpdateLastAccessedAsync(key, meta.Value, metaDictionary.Value, timeout, cancellationToken);
                return data.Value;
            }
        }


        public async Task RefreshCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var metaDictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, CacheEntry>>(GetMetadataDictionaryName(clientId, tenantId));
            if (!metaDictionary.HasValue) return;
            ConditionalValue<CacheEntry> meta;
            using (var transaction = _stateManager.CreateTransaction())
            {
                meta = await metaDictionary.Value.TryGetValueAsync(transaction, key, timeout, cancellationToken);
                if (!meta.HasValue) return;
            }
            await UpdateLastAccessedAsync(key, meta.Value, metaDictionary.Value, timeout, cancellationToken);
        }

        private async Task<bool> UpdateLastAccessedAsync(string key, CacheEntry storedEntry, IReliableDictionary<string, CacheEntry> dictionary,
            TimeSpan timeout, CancellationToken cancellationToken)
        {
            var newEntry = (CacheEntry)storedEntry.Clone();
            newEntry.LastAccessed = DateTimeOffset.UtcNow;
            using (var transaction = _stateManager.CreateTransaction())
            {
                var result = await dictionary.TryUpdateAsync(transaction, key, newEntry, storedEntry, timeout, cancellationToken);
                await transaction.CommitAsync();
                return result;
            }
        }

        public async Task DeleteCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var dataDictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, byte[]>>(GetDataDictionaryName(clientId, tenantId));
            var metaDictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, CacheEntry>>(GetMetadataDictionaryName(clientId, tenantId));
            using (var transaction = _stateManager.CreateTransaction())
            {
                if (metaDictionary.HasValue)
                {
                    await metaDictionary.Value.TryRemoveAsync(transaction, key, timeout, cancellationToken);
                };
                if (dataDictionary.HasValue)
                {
                    await dataDictionary.Value.TryRemoveAsync(transaction, key, timeout, cancellationToken);
                };
                await transaction.CommitAsync();
            }
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var transaction = _stateManager.CreateTransaction())
            {
                await EnsureCacheRegistered(clientId, tenantId, transaction, timeout, cancellationToken);
                var dataDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(GetDataDictionaryName(clientId, tenantId));
                await dataDictionary.AddOrUpdateAsync(transaction, key, value, (k, v) => value, timeout, cancellationToken);
                
                var metaDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheEntry>>(GetMetadataDictionaryName(clientId, tenantId));
                await metaDictionary.AddOrUpdateAsync(
                    transaction, 
                    key, 
                    new CacheEntry
                    {
                        AbsoluteExpiration = options.AbsoluteExpiration,
                        AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = options.SlidingExpiration,
                        Size = value.LongLength,
                        Created = DateTimeOffset.UtcNow,
                        LastAccessed = DateTimeOffset.UtcNow
                    }, 
                    (k, v) =>
                    {
                        if (options.AbsoluteExpiration.HasValue)
                        {
                            v.AbsoluteExpiration = options.AbsoluteExpiration;
                        }
                        if (options.AbsoluteExpirationRelativeToNow.HasValue)
                        {
                            v.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
                        }
                        if (options.SlidingExpiration.HasValue)
                        {
                            v.SlidingExpiration = options.SlidingExpiration;
                        }
                        v.Size = value.LongLength;
                        v.LastAccessed = DateTimeOffset.UtcNow;
                        return v;
                    }, 
                    timeout, 
                    cancellationToken);

                await transaction.CommitAsync();
            }
        }

        private async Task EnsureCacheRegistered(string clientId, string tenantId, ITransaction transaction, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var name = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheInfo>>(DictNamesKey);
            await dictionary.AddOrUpdateAsync(
                transaction,
                name,
                new CacheInfo
                {
                    ClientId = clientId, 
                    TenantId = tenantId, 
                    LastAccess = DateTime.UtcNow
                },
                (k, v) =>
                {
                    v.LastAccess = DateTime.UtcNow;
                    return v;
                },
                timeout, cancellationToken);
        }

        private static bool IsExpired(CacheEntry meta)
        {
            var secs = GetExpirationInSeconds(meta);
            if (!secs.HasValue) return false;
            return (secs.Value < 1);
        }

        private static long? GetExpirationInSeconds(CacheEntry meta)
        {
            var absoluteExpiration = GetAbsoluteExpiration(meta);
            if (absoluteExpiration.HasValue && meta.SlidingExpiration.HasValue)
            {
                return (long)Math.Min(
                    (absoluteExpiration.Value - meta.Created).TotalSeconds,
                    meta.SlidingExpiration.Value.TotalSeconds);
            }
            if (absoluteExpiration.HasValue)
            {
                return (long)(absoluteExpiration.Value - meta.Created).TotalSeconds;
            }
            if (meta.SlidingExpiration.HasValue)
            {
                return (long)meta.SlidingExpiration.Value.TotalSeconds;
            }
            return null;
        }

        private static DateTimeOffset? GetAbsoluteExpiration(CacheEntry meta)
        {
            if (meta.AbsoluteExpiration.HasValue && meta.AbsoluteExpiration <= meta.Created)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(CacheEntry.AbsoluteExpiration),
                    meta.AbsoluteExpiration.Value,
                    "The absolute expiration value must be in the future.");
            }
            var absoluteExpiration = meta.AbsoluteExpiration;
            if (meta.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = meta.Created + meta.AbsoluteExpirationRelativeToNow;
            }

            return absoluteExpiration;
        }

        private string GetCacheName(string clientId, string tenantId)
        {
            return $"C:T{tenantId}:C{clientId}";
        }

        private string GetMetadataDictionaryName(string clientId, string tenantId)
        {
            return $"M:T{tenantId}:C{clientId}";
        }

        private string GetDataDictionaryName(string clientId, string tenantId)
        {
            return $"D:T{tenantId}:C{clientId}";
        }
    }
}
