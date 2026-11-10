using Garupa.Domain.Exceptions;
using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Enums;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class SosService(ISosRepository repository, IUsuarioRepository usuarios, TimeProvider clock) : ISosService
{
    public async Task<Sos?> CriarAsync(string eventoId, int usuarioId, TipoEmergencia tipo,
        string origem, double lat, double lon, DateTime ocorridoEm, CancellationToken cancellationToken)
    {
        var sos = new Sos(eventoId, usuarioId, tipo, origem, lat, lon, ocorridoEm, clock.GetUtcNow().UtcDateTime);
        var existente = await repository.ObterPorEventoAsync(sos.EventoId, cancellationToken);
        if (existente is not null)
            return ConferirReenvio(existente, sos);
        if (await usuarios.ObterPorIdAsync(usuarioId, cancellationToken) is null)
            return null;
        var salvo = await repository.AdicionarAsync(sos, cancellationToken);
        return ConferirReenvio(salvo, sos);
    }

    private static Sos ConferirReenvio(Sos existente, Sos recebido)
    {
        if (existente.UsuarioId != recebido.UsuarioId || existente.Tipo != recebido.Tipo ||
            existente.Origem != recebido.Origem ||
            (existente.Latitude.HasValue && existente.Latitude != recebido.Latitude) ||
            (existente.Longitude.HasValue && existente.Longitude != recebido.Longitude) ||
            existente.OcorridoEm != recebido.OcorridoEm)
            throw new ConflitoSosException("Evento já utilizado com outros dados.");
        return existente;
    }

    public async Task<Sos?> AcompanharAsync(string token, CancellationToken cancellationToken)
    {
        var sos = await repository.ObterPorTokenAsync(token, cancellationToken);
        if (sos is null) return null;
        var agora = clock.GetUtcNow().UtcDateTime;
        sos.AtualizarExpiracao(agora);
        await repository.SalvarAsync(cancellationToken);
        return sos.TokenExpirou(agora) ? null : sos;
    }

    public async Task<bool> AlterarEstadoAsync(int id, bool cancelar, CancellationToken cancellationToken)
    {
        var sos = await repository.ObterPorIdAsync(id, cancellationToken);
        if (sos is null) return false;
        if (cancelar) sos.Cancelar(clock.GetUtcNow().UtcDateTime);
        else sos.Encerrar(clock.GetUtcNow().UtcDateTime);
        await repository.SalvarAsync(cancellationToken);
        return true;
    }
}
