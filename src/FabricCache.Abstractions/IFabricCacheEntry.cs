using System;

namespace MS.Extensions.Caching.ServiceFabric
{
    /// <summary>
    /// Represents a distributed cache entry.
    /// </summary>
    public interface IFabricCacheEntry
    {
        /// <summary>
        /// Gets the key of the cache entry.
        /// </summary>
        object Key { get; }

        /// <summary>
        /// Gets or set the value of the cache entry.
        /// </summary>
        byte[] Value { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or set the size of the cache entry value.
        /// </summary>
        long? Size { get; set; }
    }
}
