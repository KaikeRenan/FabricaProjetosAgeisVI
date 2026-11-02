using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using mvp.Exceptions;

namespace mvp.Middleware
{
    /// <summary>
    /// Traduz exceções de domínio em respostas ProblemDetails (RFC 7807) com o status HTTP correto,
    /// evitando try/catch repetido em cada controller. Exceções não mapeadas seguem o fluxo padrão (500).
    /// </summary>
    public class DomainExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<DomainExceptionHandler> _logger;

        public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflito de estado"),
                UpstreamUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Serviço dependente indisponível"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
                _ => (0, string.Empty)
            };

            if (status == 0)
                return false;

            _logger.LogWarning("{Title}: {Message}", title, exception.Message);

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
                cancellationToken);

            return true;
        }
    }
}
