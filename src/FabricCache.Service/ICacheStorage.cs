using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal interface ICacheStorage
    {
        Task<byte[]> GetCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
        Task RefreshCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
        Task DeleteCacheItemAsync(string key, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
        Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, string clientId, string tenantId, TimeSpan timeout, CancellationToken cancellationToken);
    }
}