using Microsoft.EntityFrameworkCore;
using OrderTrace.Api.Endpoints;
using OrderTrace.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// Infraestrutura (Database, Messaging, Background Services, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/ordertrace.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Auto apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderTraceDbContext>();
    db.Database.Migrate();
}

// Mapear endpoints automaticamente
app.MapEndpoints();

// Health check básico
app.MapGet("/", () => "OrderTrace API Running!");

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
