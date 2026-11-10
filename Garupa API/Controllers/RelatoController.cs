using System.ComponentModel.DataAnnotations;
using Garupa.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1/relatos")]
public class RelatoController(IRelatoService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Criar(RelatoRequest request, CancellationToken ct)
    {
        var relato = await service.CriarAsync(request.EventoId, request.UsuarioId, request.Tipo,
            request.Lat!.Value, request.Lon!.Value, request.Descricao, ct);
        return relato is null ? Problem(statusCode: 404, title: "Usuário não encontrado.")
            : StatusCode(201, new { relatoId = relato.Id });
    }
}
public record RelatoRequest([Required] string EventoId, [Range(1, int.MaxValue)] int UsuarioId,
    [Required, MaxLength(40)] string Tipo, [Required, Range(-90, 90)] double? Lat,
    [Required, Range(-180, 180)] double? Lon, [Required(AllowEmptyStrings = true), MaxLength(280)] string Descricao);
