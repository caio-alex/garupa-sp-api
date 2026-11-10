using System.ComponentModel.DataAnnotations;
using Garupa.Application.Interfaces;
using Garupa.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class SosController(ISosService service) : ControllerBase
{
    [HttpPost("sos")]
    public async Task<IActionResult> CriarSos(SosRequest request, CancellationToken cancellationToken)
    {
        var tipo = request.Tipo switch
        {
            "queda" => TipoEmergencia.Queda,
            "mal_estar" => TipoEmergencia.MalEstar,
            "agressao_assalto" => TipoEmergencia.AgressaoAssalto,
            _ => (TipoEmergencia?)null
        };
        if (tipo is null || request.OcorridoEm!.Value.Offset != TimeSpan.Zero)
            return Problem(statusCode: 400, title: "Tipo ou horário inválido.");
        var sos = await service.CriarAsync(request.EventoId, request.UsuarioId, tipo.Value,
            request.Origem, request.Lat!.Value, request.Lon!.Value, request.OcorridoEm!.Value.UtcDateTime, cancellationToken);
        if (sos is null) return Problem(statusCode: 404, title: "Usuário não encontrado.");
        return Created($"/api/v1/acompanhar/{sos.TokenAcompanhamento}",
            new { sosId = sos.Id, sos.TokenAcompanhamento });
    }

    [HttpPatch("sos/{id:int}/cancelar")]
    public async Task<IActionResult> CancelarSos(int id, CancellationToken cancellationToken)
        => await service.AlterarEstadoAsync(id, true, cancellationToken)
            ? NoContent() : Problem(statusCode: 404, title: "SOS não encontrado.");

    [HttpPatch("sos/{id:int}/encerrar")]
    public async Task<IActionResult> EncerrarSos(int id, CancellationToken cancellationToken)
        => await service.AlterarEstadoAsync(id, false, cancellationToken)
            ? NoContent() : Problem(statusCode: 404, title: "SOS não encontrado.");

    [HttpGet("acompanhar/{token}")]
    public async Task<IActionResult> Acompanhar(string token, CancellationToken cancellationToken)
    {
        var sos = await service.AcompanharAsync(token, cancellationToken);
        if (sos is null) return Problem(statusCode: 404, title: "Acompanhamento não encontrado.");
        return Ok(new
        {
            posicao = new { lat = sos.Latitude, lon = sos.Longitude },
            horario = sos.OcorridoEm,
            estado = sos.Estado.ToString().ToLowerInvariant()
        });
    }
}

public record SosRequest(
    [Required] string EventoId,
    [Range(1, int.MaxValue)] int UsuarioId,
    [Required] string Tipo,
    [Required] string Origem,
    [Required] DateTimeOffset? OcorridoEm,
    [Required, Range(-90, 90)] double? Lat,
    [Required, Range(-180, 180)] double? Lon);
