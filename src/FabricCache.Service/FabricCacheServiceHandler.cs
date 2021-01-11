using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MS.Extensions.Caching.ServiceFabric
{

    internal class FabricCacheServiceHandler : ICacheService, IDisposable
    {
        private readonly TimeSpan _defaultTimeout;
        private readonly ICacheStorage _storage;
        private readonly ILogger<FabricCacheServiceHandler> _logger;
        private readonly CancellationTokenSource _backgroundCts;

        public FabricCacheServiceHandler(ICacheStorage storage, ILogger<FabricCacheServiceHandler> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger;
            _defaultTimeout = TimeSpan.FromSeconds(10);
            _backgroundCts = new CancellationTokenSource();
            _logger?.LogTrace("Instance of class '{Type}' created!", nameof(FabricCacheServiceHandler));
        }

        public async Task<byte[]> GetCacheItemAsync(string key, string clientId, string tenantId)
        {
            _logger?.LogTrace("Getting cache item with key '{Key}' ...", key);
            var result = await _storage.GetCacheItemAsync(key, clientId, tenantId, _defaultTimeout, _backgroundCts.Token);
            _logger?.LogTrace("Getting cache item with key '{Key}' done!", key);
            return result;
        }

        public async Task RefreshCacheItemAsync(string key, string clientId, string tenantId)
        {
            _logger?.LogTrace("Refreshing cache item with key '{Key}' ...", key);
            await _storage.RefreshCacheItemAsync(key, clientId, tenantId, _defaultTimeout, _backgroundCts.Token);
            _logger?.LogTrace("Refreshing cache item with key '{Key}' done!", key);
        }

        public async Task DeleteCacheItemAsync(string key, string clientId, string tenantId)
        {
            _logger?.LogTrace("Deleting cache item with key '{Key}' ...", key);
            await _storage.DeleteCacheItemAsync(key, clientId, tenantId, _defaultTimeout, _backgroundCts.Token);
            _logger?.LogTrace("Deleting cache item with key '{Key}' done!", key);
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, string clientId, string tenantId)
        {
            _logger?.LogTrace("Setting cache item with key '{Key}' ...", key);
            await _storage.SetCacheItemAsync(key, value, options, clientId, tenantId, _defaultTimeout, _backgroundCts.Token);
            _logger?.LogTrace("Setting cache item with key '{Key}' done!", key);
        }

        public void Dispose()
        {
            try
            {
                _backgroundCts.Cancel();
            }
            finally
            {
                _backgroundCts.Dispose();
            }
        }
    }
}
