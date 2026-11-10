using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public interface IPerfilRepository
{
    Task<PerfilUso?> ObterAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<Distrito>> ListarDistritosAsync(CancellationToken ct);
    Task<bool> DistritosExistemAsync(int[] ids, CancellationToken ct);
    Task SalvarAsync(PerfilUso perfil, CancellationToken ct);
}
