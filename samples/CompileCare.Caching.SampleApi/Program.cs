using CompileCare.Caching.Abstractions;
using CompileCare.Caching.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddCompileCareCaching(builder.Configuration);

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.MapGet("/", () => "CompileCare HybridCache Sample API running");

app.MapGet("/forecast", async (
    HttpContext http,
    ICacheService cache,
    CancellationToken cancellationToken) =>
{
    var cacheKey = CacheKeyBuilder.ForGlobal("sample:weather-forecast");
    var result = await cache.GetOrCreateAsync(
        cacheKey,
        async ct =>
        {
            await Task.Delay(500, ct);
            return Enumerable.Range(1, 5).Select(index => new
            {
                Date = DateTime.UtcNow.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = "Cloudy"
            }).ToList();
        },
        cancellationToken: cancellationToken);

    http.Response.Headers["X-Cache-Demo"] = "HybridCache";
    return Results.Ok(result);
});

app.MapPost("/clear-cache", async (ICacheService cache, CancellationToken cancellationToken) =>
{
    await cache.RemoveAsync(CacheKeyBuilder.ForGlobal("sample:weather-forecast"), cancellationToken);
    return Results.NoContent();
});

app.Run();
