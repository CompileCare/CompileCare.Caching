using CompileCare.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace CompileCare.Caching.Services
{
    public class HybridCacheService : ICacheService
    {
        private readonly HybridCache _hybridCache;

        public HybridCacheService(HybridCache hybridCache)
        {
            _hybridCache = hybridCache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
        {
            return await _hybridCache.GetOrCreateAsync<T>(
                key,
                async _ => default!,
                cancellationToken: token);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken token = default)
        {
            var options = new HybridCacheEntryOptions
            {
                Expiration = expiration ?? TimeSpan.FromMinutes(5) // default
            };

            await _hybridCache.SetAsync(key, value!, options, cancellationToken: token);
        }

        public async Task<T> GetOrCreateAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    TimeSpan? expiration = null,
    CancellationToken token = default)
        {
            var options = new HybridCacheEntryOptions
            {
                Expiration = expiration ?? TimeSpan.FromMinutes(5)
            };

            return await _hybridCache.GetOrCreateAsync<object?, T>(
                key,
                state: null, // no state required
                async (_, ct) => await factory(ct),
                options,
                cancellationToken: token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            await _hybridCache.RemoveAsync(key, token);
        }
    }
}