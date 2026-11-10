using System.ComponentModel.DataAnnotations;
using Garupa.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class IntegracaoVestivelController(IVestivelService service) : ControllerBase
{
    [HttpPost("usuarios/{id:int}/vestivel")]
    public async Task<IActionResult> Parear(int id, PareamentoRequest request, CancellationToken ct)
        => await service.ParearAsync(id, request.DispositivoId, ct) ? NoContent() : Ausente();
    [HttpDelete("usuarios/{id:int}/vestivel")]
    public async Task<IActionResult> Desparear(int id, CancellationToken ct)
        => await service.DesparearAsync(id, ct) ? NoContent() : Ausente();
    [HttpPost("usuarios/{id:int}/alertas-vestivel")]
    public async Task<IActionResult> Alerta(int id, AlertaVestivelRequest request, CancellationToken ct)
    {
        if (request.OcorridoEm!.Value.Offset != TimeSpan.Zero) return Problem(statusCode: 400, title: "Horário inválido.");
        return await service.AlertaAsync(id, request.Tipo, request.OcorridoEm.Value.UtcDateTime, ct) ? Accepted() : Ausente();
    }
    [HttpPost("eventos-vestivel")]
    public async Task<IActionResult> Evento(EventoVestivelRequest request, CancellationToken ct)
    {
        if (request.OcorridoEm!.Value.Offset != TimeSpan.Zero) return Problem(statusCode: 400, title: "Horário inválido.");
        return await service.EventoAsync(request.DispositivoId, request.Tipo, request.OcorridoEm.Value.UtcDateTime, ct)
            ? Accepted() : Ausente();
    }
    [HttpGet("eventos-vestivel/pendentes")]
    public async Task<IActionResult> Eventos([FromQuery, Required, Range(1, int.MaxValue)] int? usuarioId, CancellationToken ct)
    {
        var eventos = await service.EventosAsync(usuarioId!.Value, ct);
        return eventos is null ? Ausente() : Ok(eventos);
    }
    [HttpGet("vestivel/{dispositivoId}/alertas/pendentes")]
    public async Task<IActionResult> Alertas(string dispositivoId, CancellationToken ct)
    {
        var alertas = await service.AlertasAsync(dispositivoId, ct);
        return alertas is null ? Ausente() : Ok(alertas);
    }
    private ObjectResult Ausente() => Problem(statusCode: 404, title: "Usuário ou pareamento não encontrado.");
}
public record PareamentoRequest([Required, MaxLength(100)] string DispositivoId);
public record AlertaVestivelRequest([Required] string Tipo, [Required] DateTimeOffset? OcorridoEm);
public record EventoVestivelRequest([Required, MaxLength(100)] string DispositivoId,
    [Required] string Tipo, [Required] DateTimeOffset? OcorridoEm);
