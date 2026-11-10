using System.ComponentModel.DataAnnotations;
using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Garupa.Api.Controllers;

[ApiController]
[Route("api/v1/usuarios/{id:int}")]
public class PrivacidadeController(IPrivacidadeService privacidade, IPerfilService perfis) : ControllerBase
{
    [HttpPut("consentimentos")]
    public async Task<IActionResult> Consentimentos(int id, ConsentimentosRequest request, CancellationToken ct)
        => await privacidade.AtualizarConsentimentosAsync(id, request.LocalizacaoEmUso!.Value,
            request.LocalizacaoEmSegundoPlano!.Value, request.FichaMedica!.Value,
            request.HistoricoDeLocais!.Value, request.PerfilDeUso!.Value, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Usuário não encontrado.");

    [HttpPut("ficha-medica")]
    public async Task<IActionResult> FichaMedica(int id, FichaMedicaRequest request, CancellationToken ct)
        => await privacidade.AtualizarFichaAsync(id, request.TipoSanguineo, request.Alergias,
            request.Condicoes, request.Medicamentos, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Usuário não encontrado.");

    [HttpPut("perfil")]
    public async Task<IActionResult> Perfil(int id, PerfilRequest request, CancellationToken ct)
        => await perfis.AtualizarAsync(id, request.TiposDeUso, request.HorariosHabituais,
            request.DistritosFrequentes, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Usuário não encontrado.");

    [HttpDelete]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
        => await privacidade.ExcluirAsync(id, ct)
            ? NoContent() : Problem(statusCode: 404, title: "Usuário não encontrado.");
}

public record ConsentimentosRequest(
    [Required] bool? LocalizacaoEmUso,
    [Required] bool? LocalizacaoEmSegundoPlano,
    [Required] bool? FichaMedica,
    [Required] bool? HistoricoDeLocais,
    [Required] bool? PerfilDeUso);

public record FichaMedicaRequest
{
    [MaxLength(3)] public string TipoSanguineo { get; init; } = "";
    [MaxLength(2000)] public string Alergias { get; init; } = "";
    [MaxLength(2000)] public string Condicoes { get; init; } = "";
    [MaxLength(2000)] public string Medicamentos { get; init; } = "";
}

public record PerfilRequest(
    [Required, MinLength(1), MaxLength(4)] string[] TiposDeUso,
    [Required, MaxLength(18)] HorarioHabitual[] HorariosHabituais,
    [Required, MaxLength(5)] int[] DistritosFrequentes);
