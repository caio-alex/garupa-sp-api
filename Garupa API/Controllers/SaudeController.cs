using Garupa.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Api.Controllers;

[ApiController]
public class SaudeController(GarupaDbContext context) : ControllerBase
{
    [HttpGet("/api/v1/saude")]
    [HttpGet("/saude")]
    public async Task<IActionResult> VerificarSaude(CancellationToken cancellationToken)
    {
        var conectado = await context.Database.CanConnectAsync(cancellationToken);
        return StatusCode(conectado ? 200 : 503,
            new { versao = "1.0", banco = conectado ? "conectado" : "indisponivel" });
    }
}
