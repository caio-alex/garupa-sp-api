using System.ComponentModel.DataAnnotations;
using Garupa.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class ConteudoController(IConteudoService conteudo, IPerfilService perfis) : ControllerBase
{
    [HttpGet("distritos")]
    public async Task<IActionResult> Distritos(CancellationToken ct)
        => Ok((await perfis.ListarDistritosAsync(ct)).Select(d => new { distritoId = d.Id, d.Nome }));

    [HttpGet("mensagens")]
    public async Task<IActionResult> Mensagens([FromQuery] string? contexto, [FromQuery] string? tipoDeUso, CancellationToken ct)
        => Ok(await conteudo.MensagensAsync(contexto, tipoDeUso, ct));

    [HttpGet("risco")]
    public async Task<IActionResult> Risco([FromQuery, Required] double? lat, [FromQuery, Required] double? lon,
        [FromQuery, Required] double? raioMetros, [FromQuery, Required] int? faixaHoraria,
        [FromQuery, Required] string tipoDia, [FromQuery, Required] bool? chuva, CancellationToken ct)
        => Ok(await conteudo.RiscoAsync(lat!.Value, lon!.Value, raioMetros!.Value, faixaHoraria!.Value, tipoDia, chuva!.Value, ct));

    [HttpGet("risco/celulas/{id}")]
    public async Task<IActionResult> Celula(string id, CancellationToken ct)
    {
        var resultado = await conteudo.DetalheAsync(id, ct);
        return resultado.Count == 0 ? Problem(statusCode: 404, title: "Célula não encontrada.") : Ok(resultado);
    }

    [HttpGet("plano-do-turno")]
    public async Task<IActionResult> Plano([FromQuery, Required] int? usuarioId,
        [FromQuery] int? faixaHoraria, [FromQuery, Required] string tipoDia,
        [FromQuery, Required] bool? chuva, CancellationToken ct)
    {
        var plano = await conteudo.PlanoAsync(usuarioId!.Value, faixaHoraria, tipoDia, chuva!.Value, ct);
        return plano is null ? Problem(statusCode: 404, title: "Usuário não encontrado.") : Ok(plano);
    }
}
