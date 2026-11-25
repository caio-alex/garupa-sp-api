using Garupa.Api.Security;
using Garupa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Garupa.Api.Controllers;

[ApiController, Authorize, Route("api/v1/seguranca")]
public sealed class SegurancaController(AuditoriaCadastro auditoria, ExplicadorGemini gemini) : ControllerBase
{
    [HttpGet("meu-evento"), EnableRateLimiting("painel-seguranca")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> MeuEvento(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var id)) return Unauthorized();
        var evento = auditoria.Ultimo(id);
        if (evento is null) return NoContent();
        var (mensagem, origem) = await gemini.Explicar(evento, ct);
        return Ok(new { evento.Data, evento.Correlacao, evento.Campos, risco = "Amarelo",
            acao = "Valores omitidos do log", mensagem, origem });
    }
}
