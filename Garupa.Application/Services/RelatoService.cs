using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Exceptions;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class RelatoService(IRelatoRepository repository, IUsuarioRepository usuarios,
    ILimiteMunicipal limite) : IRelatoService
{
    public async Task<Relato?> CriarAsync(string eventoId, int usuarioId, string tipo,
        double lat, double lon, string descricao, CancellationToken ct)
    {
        var relato = new Relato(eventoId, usuarioId, tipo, lat, lon, descricao);
        if (!limite.Contem(lat, lon)) throw new ArgumentException("Local fora do município de São Paulo.");
        var existente = await repository.ObterPorEventoAsync(relato.EventoId, ct);
        if (existente is not null) return Conferir(existente, relato);
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return null;
        return Conferir(await repository.AdicionarAsync(relato, ct), relato);
    }
    private static Relato Conferir(Relato existente, Relato recebido)
    {
        if (existente.UsuarioId != recebido.UsuarioId || existente.Tipo != recebido.Tipo ||
            existente.Latitude != recebido.Latitude || existente.Longitude != recebido.Longitude || existente.Descricao != recebido.Descricao)
            throw new ConflitoRegraException("Evento já utilizado com outros dados.");
        return existente;
    }
}
