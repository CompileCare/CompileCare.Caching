using CompileCare.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace CompileCare.Caching.Services;

public sealed class HybridCacheService(
    HybridCache hybridCache,
    IOptions<CacheSettings> settings) : ICacheService
{
    private readonly CacheSettings _settings = settings.Value;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return hybridCache.GetOrCreateAsync<T>(
            BuildKey(key),
            _ => ValueTask.FromResult<T?>(default),
            cancellationToken: cancellationToken);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ValidateExpiration(expiration);

        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _settings.DefaultExpiration,
            LocalCacheExpiration = _settings.LocalCacheExpiration
        };

        await hybridCache.SetAsync(BuildKey(key), value!, options, cancellationToken: cancellationToken);
    }

    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateExpiration(expiration);

        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _settings.DefaultExpiration,
            LocalCacheExpiration = _settings.LocalCacheExpiration
        };

        return hybridCache.GetOrCreateAsync<object?, T>(
            BuildKey(key),
            state: null,
            async (_, ct) => await factory(ct),
            options,
            cancellationToken: cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return hybridCache.RemoveAsync(BuildKey(key), cancellationToken);
    }

    private string BuildKey(string key) => $"{_settings.KeyPrefix}:{key}";

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 256 || key.Any(char.IsControl))
            throw new ArgumentException("Cache key must be 1-256 characters and contain no control characters.", nameof(key));
    }

    private static void ValidateExpiration(TimeSpan? expiration)
    {
        if (expiration is <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Cache expiration must be positive.");
    }
}