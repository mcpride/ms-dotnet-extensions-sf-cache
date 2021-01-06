using System;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal class FabricCacheInfo
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public DateTime LastAccess { get; set; }
    }
}