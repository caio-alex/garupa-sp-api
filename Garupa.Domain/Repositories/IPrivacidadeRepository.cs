using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public interface IPrivacidadeRepository
{
    Task<Consentimento?> ObterConsentimentoAsync(int usuarioId, CancellationToken ct);
    Task SalvarConsentimentoAsync(Consentimento consentimento, CancellationToken ct);
    Task SalvarFichaAsync(int usuarioId, FichaMedica ficha, CancellationToken ct);
    Task<bool> ExcluirUsuarioAsync(int usuarioId, CancellationToken ct);
}
