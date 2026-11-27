using HeapCare.Caching.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace HeapCare.Caching.Services;

public sealed class HybridCacheService(
    HybridCache hybridCache,
    IOptions<CacheSettings> settings) : ICacheService
{
    private readonly CacheSettings _settings = settings.Value;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var negativeCacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(1),
            LocalCacheExpiration = TimeSpan.FromSeconds(1)
        };

        return await hybridCache.GetOrCreateAsync<T>(
            BuildKey(key),
            async _ => default!,
            negativeCacheOptions,
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

        var effectiveExpiration = expiration ?? _settings.DefaultExpiration;
        var options = CreateEntryOptions(effectiveExpiration);

        await hybridCache.SetAsync(BuildKey(key), value!, options, cancellationToken: cancellationToken);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateExpiration(expiration);

        var effectiveExpiration = expiration ?? _settings.DefaultExpiration;
        var options = CreateEntryOptions(effectiveExpiration);

        return await hybridCache.GetOrCreateAsync<object?, T>(
            BuildKey(key),
            state: null,
            async (_, ct) => await factory(ct),
            options,
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await hybridCache.RemoveAsync(BuildKey(key), cancellationToken);
    }

    private HybridCacheEntryOptions CreateEntryOptions(TimeSpan expiration) =>
        new()
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration < _settings.LocalCacheExpiration
                ? expiration
                : _settings.LocalCacheExpiration
        };

    private string BuildKey(string key) => $"{_settings.KeyPrefix}:{key}";

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 256 || key.Any(char.IsControl))
            throw new ArgumentException("Cache key must be 1-256 characters and contain no control characters.", nameof(key));
    }

    private static void ValidateExpiration(TimeSpan? expiration)
    {
        if (expiration.HasValue && expiration.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Cache expiration must be positive.");
    }
}