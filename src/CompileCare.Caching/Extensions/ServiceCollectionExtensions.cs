using CompileCare.Caching.Abstractions;
using CompileCare.Caching.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompileCare.Caching.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCompileCareCaching(this IServiceCollection services, IConfiguration config)
        {
            var section = config.GetSection("CacheSettings");

            bool useRedis = section.GetValue<bool>("UseRedis");
            string? redisConnection = section.GetValue<string>("RedisConnection");

            TimeSpan defaultExpiration = TimeSpan.FromSeconds(
                section.GetValue<int>("DefaultExpirationSeconds", 300));

            TimeSpan localExpiration = TimeSpan.FromSeconds(
                section.GetValue<int>("LocalCacheExpirationSeconds", 200));

            if (useRedis && !string.IsNullOrEmpty(redisConnection))
            {
                services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnection);
            }

            services.AddHybridCache(opt =>
            {
                opt.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = defaultExpiration,
                    LocalCacheExpiration = localExpiration
                };
            });

            services.AddScoped<ICacheService, HybridCacheService>();

            return services;
        }
    }
}
