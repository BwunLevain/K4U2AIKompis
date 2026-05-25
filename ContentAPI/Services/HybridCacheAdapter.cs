using Microsoft.Extensions.Caching.Hybrid;

namespace ContentAPI.Services
{
    public class HybridCacheAdapter : ICacheClient
    {
        private readonly HybridCache _cache;

        public HybridCacheAdapter(HybridCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, string[]? tags = null)
        {
            // Convert Task-based factory to ValueTask-based factory expected by HybridCache
            ValueTask<T> ValueFactory(CancellationToken ct) => new ValueTask<T>(factory(ct));

            var valueTask = _cache.GetOrCreateAsync<T>(key, ValueFactory, tags: tags);
            return await valueTask.AsTask().ConfigureAwait(false);
        }

        public async Task RemoveAsync(string key)
        {
            var vt = _cache.RemoveAsync(key);
            await vt.AsTask().ConfigureAwait(false);
        }

        public async Task RemoveByTagAsync(string tag)
        {
            var vt = _cache.RemoveByTagAsync(tag);
            await vt.AsTask().ConfigureAwait(false);
        }
    }
}