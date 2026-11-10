namespace Garupa.Application.Interfaces;

public interface IPrivacidadeService
{
    Task<bool> AtualizarConsentimentosAsync(int usuarioId, bool localizacaoEmUso,
        bool localizacaoEmSegundoPlano, bool fichaMedica, bool historicoDeLocais, bool perfilDeUso, CancellationToken ct);
    Task<bool> AtualizarFichaAsync(int usuarioId, string tipoSanguineo, string alergias,
        string condicoes, string medicamentos, CancellationToken ct);
    Task<bool> ExcluirAsync(int usuarioId, CancellationToken ct);
}
