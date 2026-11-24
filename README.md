# CompileCare.Caching

Reusable .NET 10 HybridCache infrastructure for CompileCare services.

## Features

- HybridCache with in-process caching and optional Redis distributed cache.
- Validated cache settings and configurable default/local expiration.
- Shared `ICacheService` for Get, Set, GetOrCreate and Remove.
- Explicit global and tenant cache-key namespaces.
- Cancellation support and key/expiration validation.
- Sample API and Docker-based local Redis setup.

## Registration

```csharp
builder.Services.AddCompileCareCaching(builder.Configuration);
```

Configuration:

```json
{
  "CacheSettings": {
    "UseRedis": false,
    "RedisConnection": "",
    "KeyPrefix": "compilecare",
    "DefaultExpirationSeconds": 300,
    "LocalCacheExpirationSeconds": 120
  }
}
```

When `UseRedis` is true, `RedisConnection` is required. Do not commit credentials. Use environment variables or a secret manager in deployed environments.

## Usage

```csharp
var cached = await cache.GetOrCreateAsync(
    CacheKeyBuilder.ForTenant(tenantId, $"hospital:{hospitalId}:catalog"),
    async cancellationToken => await repository.LoadCatalogAsync(cancellationToken),
    cancellationToken: cancellationToken);
```

Use `CacheKeyBuilder.ForTenant` for tenant-owned data and `ForGlobal` only for intentionally shared, non-tenant-specific data. Raw keys are not tenant-safe by themselves. Avoid caching patient records, PHI, authentication tokens, or other sensitive data until a separate data-protection and threat-model review approves the use case.

## Expiration

- `DefaultExpirationSeconds` controls the default distributed expiration.
- `LocalCacheExpirationSeconds` controls the in-process copy and must be positive and no longer than the default expiration.
- Per-operation expiration overrides the default and must be positive.

## Local development

```bash
docker compose up --build
```

The local Redis container is for development only. Do not expose Redis publicly or reuse this compose configuration for production.

## Tests

```bash
dotnet test CompileCare.Caching.sln
```

## Current scope

This package provides cache infrastructure, not a cache invalidation strategy for domain entities. Consumers own tenant-safe key design, invalidation, stale-data tolerance, consistency requirements and whether a particular data type may be cached.
