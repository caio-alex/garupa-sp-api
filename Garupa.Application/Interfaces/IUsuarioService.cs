
using Garupa.Domain.Entities;

namespace Garupa.Application.Interfaces;

public interface IUsuarioService
{
    Task<Usuario> CriarAsync(
        string nome,
        string telefone,
        CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ContatoEmergencia?> AdicionarContatoAsync(
        int usuarioId,
        string nome,
        string telefone,
        CancellationToken cancellationToken = default);
}
