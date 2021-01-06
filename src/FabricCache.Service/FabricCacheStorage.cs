using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal class FabricCacheStorage : IFabricCacheStorage
    {
        private const string DictNamesKey = "caches";
        private readonly IReliableStateManager _stateManager;
        private readonly ILogger<FabricCacheStorage> _logger;

        public FabricCacheStorage(IReliableStateManager stateManager, ILogger<FabricCacheStorage> logger)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger;
        }

        public async Task<FabricCacheStorageEntry> GetCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var cacheName = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, FabricCacheStorageEntry>>(cacheName);
            if (!dictionary.HasValue) return null;
            using (var transaction = _stateManager.CreateTransaction())
            {
                var result = await dictionary.Value.TryGetValueAsync(transaction, key, timeout, cancellationToken);
                if (!result.HasValue) return null;
                //TODO: validate expiration
                return result.Value;
            }
        }

        public async Task RefreshCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var cacheName = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, FabricCacheStorageEntry>>(cacheName);
            if (!dictionary.HasValue) return;
            FabricCacheStorageEntry value;
            using (var transaction = _stateManager.CreateTransaction())
            {
                var result = await dictionary.Value.TryGetValueAsync(transaction, key, timeout, cancellationToken);
                if (!result.HasValue) return;
                value = result.Value;
            }
            //TODO: Update expiration
            //value.SlidingExpiration = 
            using (var transaction = _stateManager.CreateTransaction())
            {
                await dictionary.Value.AddOrUpdateAsync(transaction, key, value, (k, v) => value, timeout, cancellationToken);
            }
        }

        public async Task DeleteCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var cacheName = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.TryGetAsync<IReliableDictionary<string, FabricCacheStorageEntry>>(cacheName);
            if (!dictionary.HasValue) return;
            using (var transaction = _stateManager.CreateTransaction())
            {
                await dictionary.Value.TryRemoveAsync(transaction, key, timeout, cancellationToken);
            }
        }

        public async Task SetCacheItemAsync(string key, FabricCacheStorageEntry value, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var transaction = _stateManager.CreateTransaction())
            {
                var cacheName = await EnsureCacheRegistered(clientId, tenantId, transaction, timeout, cancellationToken);
                var dictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, FabricCacheStorageEntry>>(cacheName);
                await dictionary.AddOrUpdateAsync(transaction, key, value, (k, v) => value, timeout, cancellationToken);
            }
        }

        private async Task<string> EnsureCacheRegistered(string clientId, string tenantId, ITransaction transaction, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var name = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, FabricCacheInfo>>(DictNamesKey);
            await dictionary.AddOrUpdateAsync(
                transaction,
                name,
                new FabricCacheInfo
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
            return name;
        }

        private string GetCacheName(string clientId, string tenantId)
        {
            return $"C:T{tenantId}:C{clientId}";
        }

    }
}
