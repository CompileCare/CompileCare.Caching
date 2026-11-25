using CompileCare.Caching.Abstractions;
using CompileCare.Caching.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CompileCare.Caching.Tests;

public sealed class HybridCacheServiceTests
{
    private static ServiceProvider CreateServiceProvider(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["CacheSettings:UseRedis"] = "false",
            ["CacheSettings:DefaultExpirationSeconds"] = "30",
            ["CacheSettings:LocalCacheExpirationSeconds"] = "10",
            ["CacheSettings:KeyPrefix"] = "test"
        };

        if (overrides is not null)
            foreach (var item in overrides)
                values[item.Key] = item.Value;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddCompileCareCaching(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SetAndGet_ReturnsStoredValue()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        await cache.SetAsync("sample", "value");

        Assert.Equal("value", await cache.GetAsync<string>("sample"));
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenKeyDoesNotExist()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        Assert.Null(await cache.GetAsync<string>("missing"));
    }

    [Fact]
    public async Task GetAsync_AfterMissAndSet_ReturnsNewValue()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        Assert.Null(await cache.GetAsync<string>("set-after-miss"));

        await cache.SetAsync("set-after-miss", "written");

        Assert.Equal("written", await cache.GetAsync<string>("set-after-miss"));
    }

    [Fact]
    public async Task GetOrCreateAsync_InvokesFactoryOnceForRepeatedCalls()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
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
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        await cache.SetAsync("remove", "value");
        await cache.RemoveAsync("remove");

        Assert.Null(await cache.GetAsync<string>("remove"));
    }

    [Fact]
    public async Task SetAsync_RejectsNonPositiveExpiration()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => cache.SetAsync("invalid", "value", TimeSpan.Zero));
    }

    [Fact]
    public async Task GetOrCreateAsync_RejectsNullFactory()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => cache.GetOrCreateAsync<string>("invalid", null!));
    }

    [Fact]
    public void AddCompileCareCaching_RejectsRedisWithoutConnectionString()
    {
        Assert.Throws<OptionsValidationException>(() =>
            CreateServiceProvider(new Dictionary<string, string?>
            {
                ["CacheSettings:UseRedis"] = "true",
                ["CacheSettings:RedisConnection"] = ""
            }));
    }

    [Fact]
    public void AddCompileCareCaching_RejectsInvalidExpirationConfiguration()
    {
        Assert.Throws<OptionsValidationException>(() =>
            CreateServiceProvider(new Dictionary<string, string?>
            {
                ["CacheSettings:DefaultExpirationSeconds"] = "0",
                ["CacheSettings:LocalCacheExpirationSeconds"] = "1"
            }));
    }

    [Fact]
    public async Task CacheKeyBuilder_SeparatesTenantAndGlobalNamespaces()
    {
        var tenant = Guid.Parse("e4dfc2d1-808f-4a37-8e92-98a2dff25c51");

        Assert.Equal($"tenant:{tenant:N}:catalog", CacheKeyBuilder.ForTenant(tenant, "catalog"));
        Assert.Equal("global:catalog", CacheKeyBuilder.ForGlobal("catalog"));
        Assert.NotEqual(CacheKeyBuilder.ForTenant(tenant, "catalog"), CacheKeyBuilder.ForGlobal("catalog"));
    }

    [Fact]
    public async Task TenantNamespacedKeys_KeepValuesIsolatedAcrossTenantsAndGlobalScope()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        var tenantA = Guid.Parse("e4dfc2d1-808f-4a37-8e92-98a2dff25c51");
        var tenantB = Guid.Parse("b4d8f03f-2bfa-4e49-9990-e2bd682fcaf2");

        var tenantAKey = CacheKeyBuilder.ForTenant(tenantA, "catalog");
        var tenantBKey = CacheKeyBuilder.ForTenant(tenantB, "catalog");
        var globalKey = CacheKeyBuilder.ForGlobal("catalog");

        await cache.SetAsync(tenantAKey, "tenant-a-value");
        await cache.SetAsync(tenantBKey, "tenant-b-value");
        await cache.SetAsync(globalKey, "global-value");

        Assert.Equal("tenant-a-value", await cache.GetAsync<string>(tenantAKey));
        Assert.Equal("tenant-b-value", await cache.GetAsync<string>(tenantBKey));
        Assert.Equal("global-value", await cache.GetAsync<string>(globalKey));
    }

    [Fact]
    public async Task RemovingOneTenantKey_DoesNotRemoveAnotherTenantsValue()
    {
        using var provider = CreateServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        var tenantA = Guid.Parse("e4dfc2d1-808f-4a37-8e92-98a2dff25c51");
        var tenantB = Guid.Parse("b4d8f03f-2bfa-4e49-9990-e2bd682fcaf2");

        var tenantAKey = CacheKeyBuilder.ForTenant(tenantA, "patient-search");
        var tenantBKey = CacheKeyBuilder.ForTenant(tenantB, "patient-search");
        var globalKey = CacheKeyBuilder.ForGlobal("patient-search");

        await cache.SetAsync(tenantAKey, "tenant-a-patients");
        await cache.SetAsync(tenantBKey, "tenant-b-patients");
        await cache.SetAsync(globalKey, "global-patient-search");
        await cache.RemoveAsync(tenantAKey);

        Assert.Null(await cache.GetAsync<string>(tenantAKey));
        Assert.Equal("tenant-b-patients", await cache.GetAsync<string>(tenantBKey));
        Assert.Equal("global-patient-search", await cache.GetAsync<string>(globalKey));
    }

    [Fact]
    public void CacheKeyBuilder_RejectsEmptyTenantAndUnsafeKey()
    {
        Assert.Throws<ArgumentException>(() => CacheKeyBuilder.ForTenant(Guid.Empty, "key"));
        Assert.Throws<ArgumentException>(() => CacheKeyBuilder.ForGlobal("bad\nkey"));
    }
}