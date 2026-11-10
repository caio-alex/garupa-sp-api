
using Garupa.Domain.Entities;

namespace Garupa.Application.Interfaces;

public interface IUsuarioService
{
    Task<bool> AtualizarAsync(int id, string nome, string telefone, CancellationToken ct);
    Task<bool> AtualizarContatoAsync(int id, int contatoId, string nome, string telefone, CancellationToken ct);
    Task<bool> RemoverContatoAsync(int id, int contatoId, CancellationToken ct);
    Task<Usuario> CriarAsync(
        string nome,
        string telefone,
        string nomeContato,
        string telefoneContato,
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
