using Garupa.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Garupa.Api.Errors;

public class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ArgumentException => 400,
            ConflitoRegraException or DbUpdateConcurrencyException => 409,
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "23503" } } => 409,
            _ => 500
        };
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    400 => "Dados inválidos.",
                    409 => "Operação incompatível com o estado atual. Consulte o recurso e tente novamente.",
                    _ => "Não foi possível concluir a operação."
                }
            }
        });
    }
}
