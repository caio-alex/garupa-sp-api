using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Exceptions;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class PrivacidadeService(IUsuarioRepository usuarios, IPrivacidadeRepository repository,
    TimeProvider clock) : IPrivacidadeService
{
    public async Task<bool> AtualizarConsentimentosAsync(int usuarioId, bool localizacaoEmUso,
        bool localizacaoEmSegundoPlano, bool fichaMedica, bool historicoDeLocais, bool perfilDeUso, CancellationToken ct)
    {
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return false;
        var consentimento = await repository.ObterConsentimentoAsync(usuarioId, ct) ?? new Consentimento(usuarioId);
        consentimento.Atualizar(localizacaoEmUso, localizacaoEmSegundoPlano, fichaMedica,
            historicoDeLocais, perfilDeUso, clock.GetUtcNow().UtcDateTime);
        await repository.SalvarConsentimentoAsync(consentimento, ct);
        return true;
    }

    public async Task<bool> AtualizarFichaAsync(int usuarioId, string tipoSanguineo, string alergias,
        string condicoes, string medicamentos, CancellationToken ct)
    {
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return false;
        var consentimento = await repository.ObterConsentimentoAsync(usuarioId, ct);
        if (consentimento?.FichaMedica != true)
            throw new ConflitoRegraException("É necessário consentir com o armazenamento da ficha.");
        var ficha = new FichaMedica(tipoSanguineo, alergias, condicoes, medicamentos);
        await repository.SalvarFichaAsync(usuarioId, ficha, ct);
        return true;
    }

    public Task<bool> ExcluirAsync(int usuarioId, CancellationToken ct)
        => repository.ExcluirUsuarioAsync(usuarioId, ct);
}
