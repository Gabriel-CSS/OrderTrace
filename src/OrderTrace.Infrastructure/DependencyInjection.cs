using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderTrace.Infrastructure.Messaging;
using OrderTrace.Infrastructure.Messaging.NotificationService;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;
using OrderTrace.Infrastructure.PaymentGateway;
using OrderTrace.Observability;

namespace OrderTrace.Infrastructure;

/// <summary>
/// Extensões para configurar serviços de infraestrutura
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona todos os serviços de infraestrutura
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddMessaging();
        services.AddPaymentGateway();
        services.AddBackgroundServices();
        services.AddObservability(configuration);

        return services;
    }

    /// <summary>
    /// Configura o banco de dados PostgreSQL
    /// </summary>
    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada");

        services.AddDbContext<OrderTraceDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Configura serviços de mensageria e filas
    /// </summary>
    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentQueue, PaymentQueue>();
        services.AddSingleton<INotificationService, NotificationServiceMock>();

        return services;
    }

    /// <summary>
    /// Configura serviços de gateway de pagamento
    /// </summary>
    private static IServiceCollection AddPaymentGateway(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentGatewayMockService, PaymentGatewayMockService>();

        return services;
    }

    /// <summary>
    /// Configura serviços em background (BackgroundService/HostedService)
    /// </summary>
    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<PaymentProcessingService>();

        return services;
    }

    /// <summary>
    /// Configura OpenTelemetry com Tracing e Metrics
    /// </summary>
    private static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var environment = configuration["Environment"] ?? "development";
        var useOtlpExporter = configuration.GetValue<bool>("OpenTelemetry:UseOtlpExporter", false);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: ActivitySources.ServiceName,
                    serviceVersion: ActivitySources.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = environment,
                    ["host.name"] = Environment.MachineName,
                    ["deployment.environment"] = environment
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !path.Contains("/health") && !path.Contains("/metrics");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource(ActivitySources.PaymentProcessing.Name)
                    .AddSource(ActivitySources.Gateway.Name)
                    .AddSource(ActivitySources.Domain.Name)
                    .AddSource(ActivitySources.Messaging.Name)
                    .AddConsoleExporter();

                if (useOtlpExporter)
                {
                    tracing.AddOtlpExporter(otlpOptions =>
                    {
                        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4318";
                        otlpOptions.Endpoint = new Uri($"{endpoint}/v1/traces");
                        otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(ActivitySources.ServiceName + ".*");
            });

        return services;
    }
}
