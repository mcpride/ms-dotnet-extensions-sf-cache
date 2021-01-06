using System;

namespace MS.Extensions.Caching.ServiceFabric
{
    internal class FabricCacheStorageEntry : IFabricCacheEntry
    {
        /// <inheritdoc/>
        public object Key { get; }
        /// <inheritdoc/>
        public byte[] Value { get; set; }
        /// <inheritdoc/>
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        /// <inheritdoc/>
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        /// <inheritdoc/>
        public TimeSpan? SlidingExpiration { get; set; }
        /// <inheritdoc/>
        public long? Size { get; set; }
    }
}
