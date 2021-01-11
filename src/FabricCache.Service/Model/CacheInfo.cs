using System;

namespace MS.Extensions.Caching.ServiceFabric.Model
{
    internal class CacheInfo
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public DateTime LastAccess { get; set; }
    }
}