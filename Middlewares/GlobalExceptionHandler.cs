using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Exceptions;

namespace OrdersApi.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ocorreu uma exceção: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            PedidoNaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso Não Encontrado"),
            RegraDeNegocioException      => (StatusCodes.Status400BadRequest, "Regra de Negócio Violada"),
            _                            => (StatusCodes.Status500InternalServerError, "Erro Interno no Servidor")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title, 
            Detail = statusCode == StatusCodes.Status500InternalServerError 
                ? "Ocorreu um erro inesperado ao processar sua requisição." 
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}