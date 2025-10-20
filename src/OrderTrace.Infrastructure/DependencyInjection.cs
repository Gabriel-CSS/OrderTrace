using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderTrace.Infrastructure.Messaging;
using OrderTrace.Infrastructure.Messaging.NotificationService;
using OrderTrace.Infrastructure.Messaging.PaymentQueue;
using OrderTrace.Infrastructure.PaymentGateway;

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
}
