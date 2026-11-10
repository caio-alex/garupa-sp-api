using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Exceptions;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class PerfilService(IUsuarioRepository usuarios, IPrivacidadeRepository privacidade,
    IPerfilRepository repository) : IPerfilService
{
    public async Task<bool> AtualizarAsync(int usuarioId, string[] tipos, HorarioHabitual[] horarios,
        int[] distritos, CancellationToken ct)
    {
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return false;
        if ((await privacidade.ObterConsentimentoAsync(usuarioId, ct))?.PerfilDeUso != true)
            throw new ConflitoRegraException("É necessário consentir com o perfil de uso.");
        var perfil = await repository.ObterAsync(usuarioId, ct) ?? new PerfilUso(usuarioId);
        perfil.Atualizar(tipos, horarios, distritos);
        if (!await repository.DistritosExistemAsync(distritos, ct))
            throw new ArgumentException("Distrito inexistente.");
        await repository.SalvarAsync(perfil, ct);
        return true;
    }

    public Task<IReadOnlyList<Distrito>> ListarDistritosAsync(CancellationToken ct)
        => repository.ListarDistritosAsync(ct);
}
