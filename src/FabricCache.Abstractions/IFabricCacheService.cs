using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.ServiceFabric.Services.Remoting;


namespace MS.Extensions.Caching.ServiceFabric
{
    public interface IFabricCacheService : IService
    {
        Task<byte[]> GetCacheItemAsync(string key, string clientId, string tenantId);

        Task RefreshCacheItemAsync(string key, string clientId, string tenantId);

        Task DeleteCacheItemAsync(string key, string clientId, string tenantId);

        Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, string clientId, string tenantId);
    }
}
