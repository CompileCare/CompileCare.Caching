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
        var settings = section.Get<CacheSettings>() ?? new CacheSettings();
        var validationErrors = CacheSettings.GetValidationErrors(settings);
        if (validationErrors.Count > 0)
            throw new OptionsValidationException(nameof(CacheSettings), typeof(CacheSettings), validationErrors);

        services.AddOptions<CacheSettings>()
            .Bind(section)
            .Validate(settings => CacheSettings.GetValidationErrors(settings).Count == 0,
                "CacheSettings configuration is invalid.")
            .ValidateOnStart();

        if (settings.UseRedis)
        {
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