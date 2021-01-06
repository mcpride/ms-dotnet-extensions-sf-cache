using System;
using System.Threading;
using System.Threading.Tasks;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal interface IFabricCacheStorage
    {
        Task<FabricCacheStorageEntry> GetCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
        Task RefreshCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
        Task DeleteCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
        Task SetCacheItemAsync(string key, FabricCacheStorageEntry value, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
    }
}