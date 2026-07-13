using System.Text.Json.Serialization;
using TideMonitor.Core.Interfaces;
using TideMonitor.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

// Add in-memory caching
builder.Services.AddMemoryCache();

// Register HTTP clients
builder.Services.AddHttpClient<ITideService, NoaaTideService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ILocationService, NominatimGeocodingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Register NWS Beach Hazard Service
builder.Services.AddHttpClient<IBeachHazardService, NwsBeachHazardService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Configure CORS for Vue frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
