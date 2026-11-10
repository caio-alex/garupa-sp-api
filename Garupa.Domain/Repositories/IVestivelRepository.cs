using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public record EventoPendente(int EventoId, string Tipo, DateTime OcorridoEm);
public record AlertaPendente(int AlertaId, string Tipo, DateTime OcorridoEm);

public interface IVestivelRepository
{
    Task<Vestivel?> PorUsuarioAsync(int usuarioId, CancellationToken ct);
    Task<Vestivel?> PorDispositivoAsync(string dispositivoId, CancellationToken ct);
    Task ParearAsync(Vestivel vestivel, CancellationToken ct);
    Task DesparearAsync(int usuarioId, CancellationToken ct);
    Task AdicionarEventoAsync(EventoVestivel evento, CancellationToken ct);
    Task AdicionarAlertaAsync(AlertaVestivel alerta, CancellationToken ct);
    Task<IReadOnlyList<EventoPendente>> ConsumirEventosAsync(int usuarioId, DateTime agora, CancellationToken ct);
    Task<IReadOnlyList<AlertaPendente>> ConsumirAlertasAsync(string dispositivoId, DateTime agora, CancellationToken ct);
}
