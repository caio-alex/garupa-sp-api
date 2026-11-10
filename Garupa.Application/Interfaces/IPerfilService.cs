using Garupa.Domain.Entities;

namespace Garupa.Application.Interfaces;

public interface IPerfilService
{
    Task<bool> AtualizarAsync(int usuarioId, string[] tipos, HorarioHabitual[] horarios, int[] distritos, CancellationToken ct);
    Task<IReadOnlyList<Distrito>> ListarDistritosAsync(CancellationToken ct);
}
