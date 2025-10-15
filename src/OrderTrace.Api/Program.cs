using Microsoft.EntityFrameworkCore;
using OrderTrace.Api.Endpoints;
using OrderTrace.Infrastructure;
using OrderTrace.Infrastructure.Messaging;
using OrderTrace.Infrastructure.Messaging.NotificationService;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;
using OrderTrace.Infrastructure.PaymentGateway;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configura PostgreSQL
builder.Services.AddDbContext<OrderTraceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// --- Dependency Injection ---
builder.Services.AddSingleton<IPaymentQueue, PaymentQueue>();
builder.Services.AddSingleton<IPaymentGatewayMockService, PaymentGatewayMockService>();
builder.Services.AddSingleton<INotificationService, NotificationServiceMock>();
builder.Services.AddHostedService<PaymentProcessingService>();

// --- Serilog Configuration ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // nível mínimo de log
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/ordertrace.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// --- Mapear Endpoints ---
OrdersEndpoints.MapOrdersEndpoints(app);
PaymentsEndpoints.MapPaymentsEndpoints(app);
TransactionsEndpoints.MapTransactionsEndpoints(app);

// --- Auto Apply Migrations ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderTraceDbContext>();

    db.Database.Migrate();
}

app.MapGet("/", () => "OrderTrace API Running!");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
