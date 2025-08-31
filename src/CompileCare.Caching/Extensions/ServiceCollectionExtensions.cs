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
            var useRedis = Convert.ToBoolean(section["UseRedis"]);
            var redisConnection = section["RedisConnection"];
            var defaultExpiration = TimeSpan.FromSeconds(Convert.ToInt32(section["DefaultExpirationSeconds"], 300));
            var localExpiration = TimeSpan.FromSeconds(Convert.ToInt32(section["LocalCacheExpirationSeconds"]), 200);

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
