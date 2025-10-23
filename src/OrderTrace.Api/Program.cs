using Microsoft.EntityFrameworkCore;
using OrderTrace.Api.Endpoints;
using OrderTrace.Infrastructure;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI/Swagger
builder.Services.AddOpenApi();

// Infraestrutura (Database, Messaging, Background Services, Observability, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Serilog com logs estruturados e correlação com traces
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "OrderTrace")
    .Enrich.WithProperty("Environment", builder.Configuration["Environment"] ?? "development")
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithSpan()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        "logs/ordertrace-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
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
