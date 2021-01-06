using System.Collections.Generic;
using System.Fabric;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using MS.Extensions.Caching.ServiceFabric;

namespace FabricCache
{
    internal sealed class FabricCacheService : StatefulService
    {
        private readonly ServiceProvider _serviceProvider;

        public FabricCacheService(StatefulServiceContext context)
            : base(context)
        {
            _serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddSingleton(StateManager)
                .AddFabricCacheServices()
                .BuildServiceProvider();
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(context => 
                    new FabricTransportServiceRemotingListener(
                        context, 
                        _serviceProvider.GetRequiredService<IFabricCacheService>()),
                    "ServiceEndpoint")
            };
        }
    }
}
