using CompileCare.Caching.Abstractions;
using CompileCare.Caching.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CompileCare.Caching.Tests;

public sealed class HybridCacheServiceTests
{
    private static ICacheService CreateCacheService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:UseRedis"] = "false",
                ["CacheSettings:DefaultExpirationSeconds"] = "30",
                ["CacheSettings:LocalCacheExpirationSeconds"] = "10",
                ["CacheSettings:KeyPrefix"] = "test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCompileCareCaching(configuration);
        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task SetAndGet_ReturnsStoredValue()
    {
        var cache = CreateCacheService();
        await cache.SetAsync("sample", "value");

        Assert.Equal("value", await cache.GetAsync<string>("sample"));
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyDoesNotExist()
    {
        var cache = CreateCacheService();

        Assert.Null(await cache.GetAsync<string>("missing"));
    }

    [Fact]
    public async Task GetOrCreateAsync_InvokesFactoryOnceForRepeatedCalls()
    {
        var cache = CreateCacheService();
        var factoryCalls = 0;

        Task<string> Factory(CancellationToken _) 
        {
            Interlocked.Increment(ref factoryCalls);
            return Task.FromResult("created");
        }

        Assert.Equal("created", await cache.GetOrCreateAsync("factory", Factory));
        Assert.Equal("created", await cache.GetOrCreateAsync("factory", Factory));
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntry()
    {
        var cache = CreateCacheService();
        await cache.SetAsync("remove", "value");
        await cache.RemoveAsync("remove");

        Assert.Null(await cache.GetAsync<string>("remove"));
    }

    [Fact]
    public async Task SetAsync_RejectsNonPositiveExpiration()
        => await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => CreateCacheService().SetAsync("invalid", "value", TimeSpan.Zero));

    [Fact]
    public async Task CacheKeyBuilder_SeparatesTenantAndGlobalNamespaces()
    {
        var tenant = Guid.Parse("e4dfc2d1-808f-4a37-8e92-98a2dff25c51");

        Assert.Equal($"tenant:{tenant:N}:catalog", CacheKeyBuilder.ForTenant(tenant, "catalog"));
        Assert.Equal("global:catalog", CacheKeyBuilder.ForGlobal("catalog"));
        Assert.NotEqual(CacheKeyBuilder.ForTenant(tenant, "catalog"), CacheKeyBuilder.ForGlobal("catalog"));
    }

    [Fact]
    public void CacheKeyBuilder_RejectsEmptyTenantAndUnsafeKey()
    {
        Assert.Throws<ArgumentException>(() => CacheKeyBuilder.ForTenant(Guid.Empty, "key"));
        Assert.Throws<ArgumentException>(() => CacheKeyBuilder.ForGlobal("bad\nkey"));
    }
}