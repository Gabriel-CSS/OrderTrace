using System.Reflection;

namespace OrderTrace.Api.Endpoints;

/// <summary>
/// Extensões para registrar endpoints automaticamente
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Mapeia todos os endpoints que implementam IEndpointMapper
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointMapperType = typeof(IEndpointMapper);

        var endpointMappers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(endpointMapperType));

        foreach (var mapper in endpointMappers)
        {
            var mapMethod = mapper.GetMethod(nameof(IEndpointMapper.MapEndpoints),
                BindingFlags.Public | BindingFlags.Static);

            mapMethod?.Invoke(null, new[] { app });
        }

        return app;
    }
}
