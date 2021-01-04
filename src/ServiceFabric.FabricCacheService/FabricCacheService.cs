using System.Collections.Generic;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal sealed class FabricCacheService : StatefulService
    {
        public FabricCacheService(StatefulServiceContext context)
            : base(context)
        { }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(context => 
                    new FabricTransportServiceRemotingListener(
                        context, 
                        new FabricCacheServiceHandler(StateManager,null)),
                    "ServiceEndpoint")
            };
        }
    }
}
