namespace OrderTrace.Api.Endpoints;

/// <summary>
/// Interface para classes que definem endpoints da API
/// </summary>
public interface IEndpointMapper
{
    /// <summary>
    /// Mapeia os endpoints para a aplicação
    /// </summary>
    static abstract void MapEndpoints(IEndpointRouteBuilder app);
}
