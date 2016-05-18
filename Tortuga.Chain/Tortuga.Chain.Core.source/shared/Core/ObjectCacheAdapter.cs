using System;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Tortuga.Chain.Core
{

    /// <summary>
    /// Class ObjectCacheAdapter.
    /// </summary>
    /// <seealso cref="ICacheAdapter" />
    public class ObjectCacheAdapter : ICacheAdapter
    {
        readonly ObjectCache m_ObjectCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectCacheAdapter"/> class.
        /// </summary>
        /// <param name="objectCache">The object cache.</param>
        public ObjectCacheAdapter(ObjectCache objectCache)
        {
            m_ObjectCache = objectCache;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            var memoryCache = m_ObjectCache as MemoryCache;
            memoryCache?.Trim(100); //this wont' actually trim 100%, but it helps
            foreach (var item in m_ObjectCache)
                m_ObjectCache.Remove(item.Key);
        }

        /// <summary>
        /// Clears the cache asynchronously.
        /// </summary>
        /// <returns>Task.</returns>
        public Task ClearAsync()
        {
            Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invalidates the cache.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Invalidate(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentException($"{nameof(cacheKey)} is null or empty.", nameof(cacheKey));

            m_ObjectCache.Remove(cacheKey);
        }

        /// <summary>
        /// Invalidates the cache asynchronously.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <returns>Task.</returns>
        public Task InvalidateAsync(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentException($"{nameof(cacheKey)} is null or empty.", nameof(cacheKey));

            m_ObjectCache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tries the read from cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public bool TryRead<T>(string cacheKey, out T result)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentException($"{nameof(cacheKey)} is null or empty.", nameof(cacheKey));

            var cacheItem = m_ObjectCache.GetCacheItem(cacheKey, null);
            if (cacheItem == null)
            {
                result = default(T);
                return false;
            }

            //Nulls can't be stored in a cache, so we simulate it using NullObject.Default.
            if (cacheItem.Value == NullObject.Default)
            {
                result = default(T);
                return true;
            }

            if (!(cacheItem.Value is T))
                throw new InvalidOperationException($"Cache is corrupted. Cache Key \"{cacheKey}\" is a {cacheItem.Value.GetType().Name} not a {typeof(T).Name}");

            result = (T)cacheItem.Value;

            return true;
        }

#pragma warning disable CS1998 
        /// <summary>
        /// try read from cache as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <returns>Task&lt;Tuple&lt;System.Boolean, System.Object&gt;&gt;.</returns>
        public async Task<CacheReadResult<T>> TryReadAsync<T>(string cacheKey)
        {
            T result;
            bool result2 = TryRead(cacheKey, out result);
            return new CacheReadResult<T>(result2, result);
        }
#pragma warning restore CS1998

        /// <summary>
        /// Writes to cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="policy">The policy.</param>
        public void Write(string key, object value, CachePolicy policy)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException($"{nameof(key)} is null or empty.", nameof(key));

            //Nulls can't be stored in a cache, so we simulate it using NullObject.Default.
            if (value == null)
                value = NullObject.Default;

            var mappedPolicy = new CacheItemPolicy();
            if (policy != null)
            {
                if (policy.AbsoluteExpiration.HasValue)
                    mappedPolicy.AbsoluteExpiration = policy.AbsoluteExpiration.Value;
                if (policy.SlidingExpiration.HasValue)
                    mappedPolicy.SlidingExpiration = policy.SlidingExpiration.Value;
            }

            m_ObjectCache.Set(new CacheItem(key, value), mappedPolicy);
        }

        /// <summary>
        /// Writes to cache asynchronously.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="policy">The policy.</param>
        /// <returns>Task.</returns>
        public Task WriteAsync(string key, object value, CachePolicy policy)
        {
            Write(key, value, policy);
            return Task.CompletedTask;
        }

        private class NullObject
        {
            public static readonly NullObject Default = new NullObject();

            private NullObject() { }
        }
    }



}
