using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal class CacheData
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public DateTime LastAccess { get; set; }
    }

    public class FabricCacheServiceHandler : IFabricCacheService
    {
        private readonly TimeSpan _defaultTimeout;
        private const string DictNamesKey = "caches";
        private readonly IReliableStateManager _stateManager;
        private readonly ILogger<FabricCacheServiceHandler> _logger;

        public FabricCacheServiceHandler(IReliableStateManager stateManager, ILogger<FabricCacheServiceHandler> logger)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger;
            _defaultTimeout = TimeSpan.FromSeconds(10);
            _logger?.LogTrace("Instance of class '{Type}' created!", nameof(FabricCacheServiceHandler));
        }

        public async Task<byte[]> GetCacheItemAsync(string key, string clientId, string tenantId)
        {
            _logger?.LogTrace("Getting cache item with key '{Key}' ...", key);
            var cacheName = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);
            using (var tx = _stateManager.CreateTransaction())
            {
                var result = await dictionary.TryGetValueAsync(tx, key, _defaultTimeout, new CancellationToken());
                return result.HasValue ? result.Value : null;
            }
            _logger?.LogTrace("Getting cache item with key '{Key}' done!", key);
        }

        public async Task RefreshCacheItemAsync(string key, string clientId, string tenantId)
        {
            _logger?.LogTrace("Refreshing cache item with key '{Key}' ...", key);
            throw new NotImplementedException();
            _logger?.LogTrace("Refreshing cache item with key '{Key}' done!", key);
        }

        public async Task DeleteCacheItemAsync(string key, string clientId, string tenantId)
        {
            _logger?.LogTrace("Deleting cache item with key '{Key}' ...", key);
            throw new NotImplementedException();
            _logger?.LogTrace("Deleting cache item with key '{Key}' done!", key);
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, string clientId, string tenantId)
        {
            _logger?.LogTrace("Setting cache item with key '{Key}' ...", key);
            throw new NotImplementedException();
            _logger?.LogTrace("Setting cache item with key '{Key}' done!", key);
        }

        private async Task<string> EnsureCacheRegistered(string clientId, string tenantId, ITransaction transaction)
        {
            var name = GetCacheName(clientId, tenantId);
            var dictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheData>>(DictNamesKey);
            await dictionary.AddOrUpdateAsync(
                transaction, 
                name, 
                new CacheData {ClientId = clientId, TenantId = tenantId, LastAccess = DateTime.UtcNow},
                (k, v) =>
                {
                    v.LastAccess = DateTime.UtcNow;
                    return v;
                }, 
                _defaultTimeout, new CancellationToken());
            return name;
        }

        private string GetCacheName(string clientId, string tenantId)
        {
            return $"C:T{tenantId}:C{clientId}";
        }

    }
}
