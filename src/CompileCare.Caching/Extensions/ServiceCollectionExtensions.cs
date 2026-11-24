using CompileCare.Caching.Abstractions;
using CompileCare.Caching.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CompileCare.Caching.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCompileCareCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(CacheSettings.SectionName);
        services.AddOptions<CacheSettings>()
            .Bind(section)
            .Validate(settings => settings.DefaultExpiration > TimeSpan.Zero,
                "Cache default expiration must be positive.")
            .Validate(settings => settings.LocalCacheExpiration > TimeSpan.Zero,
                "Cache local expiration must be positive.")
            .Validate(settings => settings.LocalCacheExpiration <= settings.DefaultExpiration,
                "Local cache expiration cannot exceed distributed cache expiration.")
            .Validate(settings => !settings.UseRedis || !string.IsNullOrWhiteSpace(settings.RedisConnection),
                "RedisConnection is required when UseRedis is enabled.")
            .Validate(settings => CacheSettings.IsValidKeyPrefix(settings.KeyPrefix),
                "Cache key prefix must contain 1-64 ASCII letters, digits, dots, underscores or hyphens.")
            .ValidateOnStart();

        var settings = section.Get<CacheSettings>() ?? new CacheSettings();
        if (settings.UseRedis)
        {
            if (string.IsNullOrWhiteSpace(settings.RedisConnection))
                throw new InvalidOperationException("RedisConnection is required when UseRedis is enabled.");

            services.AddStackExchangeRedisCache(options =>
                options.Configuration = settings.RedisConnection);
        }

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = settings.DefaultExpiration,
                LocalCacheExpiration = settings.LocalCacheExpiration
            };
        });

        services.AddScoped<ICacheService, HybridCacheService>();
        return services;
    }
}