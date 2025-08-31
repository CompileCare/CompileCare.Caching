using CompileCare.Caching.Abstractions;
using CompileCare.Caching.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCompileCareCaching(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast =  Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast");

app.MapGet("/", () => "CompileCare HybridCache Sample API running");

app.MapGet("/forecast", async (ICacheService cache, CancellationToken token) =>
{
    var result = await cache.GetOrCreateAsync(
        "weather_forecast",
        async ct =>
        {
            await Task.Delay(500, ct); // simulate expensive work
            return Enumerable.Range(1, 5).Select(i => new
            {
                Date = DateTime.UtcNow.AddDays(i),
                TempC = Random.Shared.Next(-20, 55),
                Summary = "Cloudy"
            }).ToList();
        },
        TimeSpan.FromSeconds(60),
        token);

    return Results.Ok(result);
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
