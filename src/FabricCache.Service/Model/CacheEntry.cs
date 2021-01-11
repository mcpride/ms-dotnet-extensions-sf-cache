using System;
using Microsoft.Extensions.Caching.Memory;

namespace MS.Extensions.Caching.ServiceFabric.Model
{
    internal class CacheEntry: ICloneable
    {
        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }
        /// <summary>
        /// Gets or sets the creation time for the cache entry.
        /// </summary>
        public DateTimeOffset Created { get; set; }
        /// <summary>
        /// Gets or set the size of the cache entry value.
        /// </summary>
        public long? Size { get; set; }
        /// <summary>
        /// Gets or sets the time of last access for the cache entry.
        /// </summary>
        public DateTimeOffset LastAccessed { get; set; }
        /// <summary>
        /// Gets or sets the eviction reason for the cache entry.
        /// </summary>
        public EvictionReason EvictionReason { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
