using System.Diagnostics;

namespace OrderTrace.Observability;

/// <summary>
/// Define os ActivitySources customizados para instrumentação distribuída
/// </summary>
public static class ActivitySources
{
    public const string ServiceName = "OrderTrace";
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// ActivitySource para operações de processamento de pagamentos
    /// </summary>
    public static readonly ActivitySource PaymentProcessing = new($"{ServiceName}.PaymentProcessing", ServiceVersion);

    /// <summary>
    /// ActivitySource para operações do gateway de pagamento
    /// </summary>
    public static readonly ActivitySource Gateway = new($"{ServiceName}.Gateway", ServiceVersion);

    /// <summary>
    /// ActivitySource para operações de domínio
    /// </summary>
    public static readonly ActivitySource Domain = new($"{ServiceName}.Domain", ServiceVersion);

    /// <summary>
    /// ActivitySource para operações de mensageria/fila
    /// </summary>
    public static readonly ActivitySource Messaging = new($"{ServiceName}.Messaging", ServiceVersion);
}

/// <summary>
/// Extensões para Activity (OpenTelemetry)
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Registra uma exceção no Activity/Span seguindo as convenções do OpenTelemetry
    /// </summary>
    public static Activity? RecordException(this Activity? activity, Exception exception)
    {
        if (activity == null) return null;

        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);

        if (exception.InnerException != null)
        {
            activity.SetTag("exception.inner.type", exception.InnerException.GetType().FullName);
            activity.SetTag("exception.inner.message", exception.InnerException.Message);
        }

        return activity;
    }
}
