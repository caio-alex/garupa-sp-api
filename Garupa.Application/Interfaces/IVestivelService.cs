using Garupa.Domain.Repositories;

namespace Garupa.Application.Interfaces;

public interface IVestivelService
{
    Task<bool> ParearAsync(int usuarioId, string dispositivoId, CancellationToken ct);
    Task<bool> DesparearAsync(int usuarioId, CancellationToken ct);
    Task<bool> EventoAsync(int usuarioId, string dispositivoId, string tipo, DateTime ocorridoEm, CancellationToken ct);
    Task<bool> AlertaAsync(int usuarioId, string tipo, DateTime ocorridoEm, CancellationToken ct);
    Task<IReadOnlyList<EventoPendente>?> EventosAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<AlertaPendente>?> AlertasAsync(int usuarioId, string dispositivoId, CancellationToken ct);
}
