using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using System.Text.Json;

namespace Garupa.Application.Services;

public class ConteudoService(IConteudoRepository repository, IPerfilRepository perfis,
    IUsuarioRepository usuarios, TimeProvider clock) : IConteudoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        return LimitarPontos(await repository.RiscoPorRaioAsync(lat, lon, raio, faixa, tipoDia, chuva, ct));
    }

    public Task<IReadOnlyList<CelulaRisco>> DetalheAsync(string id, CancellationToken ct)
        => repository.DetalheCelulaAsync(id, ct);

    public async Task<PlanoTurno?> PlanoAsync(int usuarioId, int? faixa, string tipoDia, bool chuva, CancellationToken ct)
    {
        if (!ContextoRisco.TipoDiaValido(tipoDia) || faixa is < 0 or > 5)
            throw new ArgumentException("Contexto inválido.");
        if (await usuarios.ObterPorIdAsync(usuarioId, ct) is null) return null;
        var perfil = await perfis.ObterAsync(usuarioId, ct);
        var (faixaSelecionada, origemFaixa) = SelecionarFaixa(faixa, tipoDia, perfil);
        var distritos = perfil?.Distritos.Select(d => d.DistritoId).ToArray() ?? [];
        var pontos = await repository.RiscoPorDistritosAsync(distritos, faixaSelecionada, tipoDia, chuva, ct);
        var contexto = chuva ? "chuva" : faixaSelecionada is 0 or 5 ? "noite" : tipoDia == "domingo" ? "domingo" : "geral";
        var mensagens = await repository.MensagensAsync(contexto, perfil?.ObterTipos() ?? [], ct);
        var selecionadas = mensagens.OrderByDescending(m => m.Contexto == contexto)
            .ThenByDescending(m => m.TipoDeUso is not null).ThenBy(m => m.Id).Take(2)
            .Select(m => new MensagemResposta(m.Id, m.Texto, m.Fonte)).ToArray();
        return new PlanoTurno(LimitarPontos(pontos), selecionadas, faixaSelecionada, origemFaixa);
    }

    private static IReadOnlyList<PontoCritico> LimitarPontos(IReadOnlyList<CelulaRisco> celulas)
    {
        // Reserva espaço para as duas mensagens e os metadados dentro dos 200 KB do plano.
        const int limiteBytes = 190000;
        var usados = 2;
        var resultado = new List<PontoCritico>();
        foreach (var celula in celulas)
        {
            var ponto = Ponto(celula);
            var tamanho = JsonSerializer.SerializeToUtf8Bytes(ponto, JsonOptions).Length + (resultado.Count > 0 ? 1 : 0);
            if (usados + tamanho > limiteBytes) break;
            resultado.Add(ponto);
            usados += tamanho;
        }
        return resultado;
    }

    private (int Faixa, string Origem) SelecionarFaixa(int? informada, string tipoDia, PerfilUso? perfil)
    {
        if (informada.HasValue) return (informada.Value, "consulta");
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var atual = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zona).Hour / 4;
        var habitual = perfil?.ObterHorarios().Where(h => h.TipoDia == tipoDia)
            .OrderBy(h => (h.FaixaHoraria - atual + 6) % 6).FirstOrDefault();
        return habitual is null ? (atual, "horario_atual") : (habitual.FaixaHoraria, "perfil");
    }

    private static PontoCritico Ponto(CelulaRisco c) => new(c.CelulaId, c.CentroLat, c.CentroLon,
        c.Score, c.Percentil, c.ViaReferencia, c.DistritoId, c.VersaoModelo,
        c.Chuva ? "Histórico de sinistros neste horário com chuva. Reduza a velocidade."
            : "Histórico de sinistros neste horário. Mantenha distância e atenção.");
}
