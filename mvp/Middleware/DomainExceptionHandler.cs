using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvp.Exceptions;
using Npgsql;

namespace mvp.Middleware
{
    public class DomainExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<DomainExceptionHandler> _logger;

        public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, title, detail) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflito de estado", exception.Message),
                UpstreamUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Serviço dependente indisponível", exception.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autorizado", exception.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida", exception.Message),

                DbUpdateException { InnerException: PostgresException { SqlState: "23503" } pg }
                    => (StatusCodes.Status400BadRequest, "Referência inexistente",
                        $"Um dos registros referenciados não existe ({pg.ConstraintName})."),

                DbUpdateException { InnerException: PostgresException { SqlState: "23505" } pg }
                    => (StatusCodes.Status409Conflict, "Registro duplicado",
                        $"Já existe um registro com esses dados ({pg.ConstraintName})."),

                _ => (0, string.Empty, string.Empty)
            };

            if (status == 0)
                return false;

            _logger.LogWarning("{Title}: {Message}", title, exception.Message);

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = status, Title = title, Detail = detail },
                cancellationToken);

            return true;
        }
    }
}
