using System.Text.Json.Serialization;
using SmartX.Api.Repositories;
using SmartX.Api.Services;
using SmartX.Domain.Repositories;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(args);

string clientOrigin =
    builder.Configuration["ClientOrigin"] ??
    "https://localhost:7187";

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<
    ISensorProfileRepository,
    InMemorySensorProfileRepository>();

builder.Services.AddSingleton(
    new TelemetryHistoryStore(
        capacityPerType: 1_000));

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "SmartXClient",
        policy =>
        {
            policy
                .WithOrigins(clientOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("SmartXClient");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapGet(
    "/",
    () => Results.Ok(
        new
        {
            application = "Smart-X IoT Gateway",
            status = "Online",
            apiVersion = "1.0",
            healthEndpoint = "/api/gateway/health",
            startupEndpoint = "/api/gateway/startup"
        }));

app.Run();

/// <summary>
/// Exposes the application entry point for integration testing.
/// </summary>
public partial class Program
{
}