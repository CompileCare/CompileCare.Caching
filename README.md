# CompileCare.Caching
Caching library will be used across the projects

✅ Uses HybridCache (memory + Redis).
✅ Is configurable via settings.
✅ Can be reused by all microservices by just adding the NuGet package.
✅ Provides observability (headers) to debug cache hits/misses.
✅ Works inside Docker for local dev & can scale in production.


## Features
- Hybrid caching (in-memory + Redis)
- Configurable via `appsettings.json`
- Minimal API demo
- Docker support
- NuGet-ready project structure

## Caching Architecture (Mermaid Diagram)

```mermaid
flowchart TD
    Client[Client Request] --> CacheService[ICacheService]
    CacheService -->|Cache Hit| MemoryCache[(In-Memory Cache)]
    CacheService -->|Cache Miss| Redis[(Redis Cache)]
    Redis -->|If Not Found| API[Source API/DB]
    API --> Redis
    Redis --> CacheService
    CacheService --> Client