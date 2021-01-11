using Microsoft.Extensions.DependencyInjection;

namespace MS.Extensions.Caching.ServiceFabric
{
    public static class FabricCacheExtensions
    {
        public static IServiceCollection AddFabricCacheServices(this IServiceCollection services)
        {
            services.AddTransient<ICacheStorage, FabricCacheStorage>();
            services.AddTransient<ICacheService, FabricCacheServiceHandler>();
            return services;
        }
    }
}