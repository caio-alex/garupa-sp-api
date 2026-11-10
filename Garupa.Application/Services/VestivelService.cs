using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Exceptions;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class VestivelService(IVestivelRepository repository, IUsuarioRepository usuarios, TimeProvider clock) : IVestivelService
{
    public async Task<bool> ParearAsync(int usuarioId, string dispositivoId, CancellationToken ct)
    {
        Vestivel.ValidarDispositivo(dispositivoId);
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return false;
        var existente = await repository.PorDispositivoAsync(dispositivoId, ct);
        if (existente is not null && existente.UsuarioId != usuarioId)
            throw new ConflitoRegraException("Dispositivo já pareado.");
        var atual = await repository.PorUsuarioAsync(usuarioId, ct);
        if (atual?.DispositivoId == dispositivoId) return true;
        await repository.ParearAsync(new Vestivel(usuarioId, dispositivoId), ct);
        return true;
    }
    public async Task<bool> DesparearAsync(int usuarioId, CancellationToken ct)
    {
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return false;
        await repository.DesparearAsync(usuarioId, ct);
        return true;
    }
    public async Task<bool> EventoAsync(string dispositivoId, string tipo, DateTime ocorridoEm, CancellationToken ct)
    {
        var vestivel = await repository.PorDispositivoAsync(dispositivoId, ct);
        if (vestivel is null) return false;
        var evento = new EventoVestivel(vestivel.UsuarioId, dispositivoId, tipo, ocorridoEm, clock.GetUtcNow().UtcDateTime);
        await repository.AdicionarEventoAsync(evento, ct);
        return true;
    }
    public async Task<bool> AlertaAsync(int usuarioId, string tipo, DateTime ocorridoEm, CancellationToken ct)
    {
        var vestivel = await repository.PorUsuarioAsync(usuarioId, ct);
        if (vestivel is null) return false;
        var alerta = new AlertaVestivel(usuarioId, vestivel.DispositivoId, tipo, ocorridoEm, clock.GetUtcNow().UtcDateTime);
        await repository.AdicionarAlertaAsync(alerta, ct);
        return true;
    }
    public async Task<IReadOnlyList<EventoPendente>?> EventosAsync(int usuarioId, CancellationToken ct)
    {
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return null;
        return await repository.ConsumirEventosAsync(usuarioId, clock.GetUtcNow().UtcDateTime, ct);
    }
    public async Task<IReadOnlyList<AlertaPendente>?> AlertasAsync(string dispositivoId, CancellationToken ct)
    {
        if (await repository.PorDispositivoAsync(dispositivoId, ct) is null) return null;
        return await repository.ConsumirAlertasAsync(dispositivoId, clock.GetUtcNow().UtcDateTime, ct);
    }
}
