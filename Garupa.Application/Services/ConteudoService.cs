using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;

namespace Garupa.Application.Services;

public class ConteudoService(IConteudoRepository repository, IPerfilRepository perfis,
    IUsuarioRepository usuarios) : IConteudoService
{
    public async Task<IReadOnlyList<MensagemResposta>> MensagensAsync(string? contexto, string? tipoDeUso, CancellationToken ct)
    {
        if ((contexto is not null && !Mensagem.ContextoValido(contexto)) ||
            (tipoDeUso is not null && !PerfilUso.TiposPermitidos.Contains(tipoDeUso)))
            throw new ArgumentException("Filtro inválido.");
        return (await repository.MensagensAsync(contexto, tipoDeUso is null ? PerfilUso.TiposPermitidos : [tipoDeUso], ct))
            .Where(m => contexto is null || m.Contexto == contexto)
            .Select(m => new MensagemResposta(m.Id, m.Texto, m.Fonte)).ToArray();
    }

    public async Task<IReadOnlyList<PontoCritico>> RiscoAsync(double lat, double lon, double raio,
        int faixa, string tipoDia, bool chuva, CancellationToken ct)
    {
        ContextoRisco.Validar(faixa, tipoDia);
        Posicao.Validar(lat, lon);
        if (!double.IsFinite(raio) || raio is < 1 or > 50000) throw new ArgumentException("Raio inválido.");
        return (await repository.RiscoPorRaioAsync(lat, lon, raio, faixa, tipoDia, chuva, ct)).Select(Ponto).ToArray();
    }

    public Task<IReadOnlyList<CelulaRisco>> DetalheAsync(string id, CancellationToken ct)
        => repository.DetalheCelulaAsync(id, ct);

    public async Task<PlanoTurno?> PlanoAsync(int usuarioId, int faixa, string tipoDia, bool chuva, CancellationToken ct)
    {
        ContextoRisco.Validar(faixa, tipoDia);
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return null;
        var perfil = await perfis.ObterAsync(usuarioId, ct);
        var distritos = perfil?.Distritos.Select(d => d.DistritoId).ToArray() ?? [];
        var pontos = await repository.RiscoPorDistritosAsync(distritos, faixa, tipoDia, chuva, ct);
        var contexto = chuva ? "chuva" : faixa is 0 or 5 ? "noite" : tipoDia == "domingo" ? "domingo" : "geral";
        var mensagens = await repository.MensagensAsync(contexto, perfil?.ObterTipos() ?? [], ct);
        var selecionadas = mensagens.OrderByDescending(m => m.Contexto == contexto)
            .ThenByDescending(m => m.TipoDeUso is not null).ThenBy(m => m.Id).Take(2)
            .Select(m => new MensagemResposta(m.Id, m.Texto, m.Fonte)).ToArray();
        return new PlanoTurno(pontos.Select(Ponto).ToArray(), selecionadas);
    }

    private static PontoCritico Ponto(CelulaRisco c) => new(c.CelulaId, c.CentroLat, c.CentroLon,
        c.Score, c.Percentil, c.ViaReferencia, c.DistritoId, c.VersaoModelo,
        c.Chuva ? "Histórico de sinistros neste horário com chuva. Reduza a velocidade."
            : "Histórico de sinistros neste horário. Mantenha distância e atenção.");
}
