
using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default);

    Task SalvarAlteracoesAsync(
        CancellationToken cancellationToken = default);
}
