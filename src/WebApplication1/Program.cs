using Grpc.Net.Client;
using GrpcService1;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
});
builder.Services.AddScoped<Greeter.GreeterClient>(x =>
{
    var address = builder.Configuration.GetServiceUri("grpcservice1")?.AbsoluteUri ?? throw new NullReferenceException("GrpcService1 not found");
    var channel = GrpcChannel.ForAddress(address);

    return new(channel);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (IDistributedCache cache) =>
{
    var forecasts = Array.Empty<WeatherForecast>();
    var cachedValue = await cache.GetStringAsync("weather");

    if (cachedValue is null)
    {
        forecasts = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateTime.Now.AddDays(index),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();

        cachedValue = JsonSerializer.Serialize(forecasts);

        await cache.SetStringAsync("weather", cachedValue, new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
        });
    }

    return forecasts;
})
.Produces<IEnumerable<WeatherForecast>>()
.WithName("GetWeatherForecast");

app.MapGet("/sayhello", async (IDistributedCache cache, Greeter.GreeterClient greeterClient, string name) =>
{
    var reply = await greeterClient.SayHelloAsync(new() { Name = name }, null);
    return reply;
})
.Produces<HelloReply>()
.WithName("GetGreeting");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}