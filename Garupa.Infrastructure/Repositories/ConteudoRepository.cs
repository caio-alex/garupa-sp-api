using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Repositories;

public class ConteudoRepository(GarupaDbContext context) : IConteudoRepository
{
    public async Task<IReadOnlyList<Mensagem>> MensagensAsync(string? contexto, string[] tipos, CancellationToken ct)
        => await context.Mensagens.AsNoTracking().Where(m => m.Ativa &&
            (contexto == null || m.Contexto == contexto || m.Contexto == "geral") &&
            (m.TipoDeUso == null || tipos.Contains(m.TipoDeUso))).OrderBy(m => m.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<CelulaRisco>> RiscoPorRaioAsync(double lat, double lon, double raio,
        int faixa, string tipoDia, bool chuva, CancellationToken ct)
    {
        var deltaLat = raio / 110000;
        var deltaLon = Math.Min(180, deltaLat / Math.Max(0.001, Math.Cos(lat * Math.PI / 180)));
        var candidatos = await Contexto(faixa, tipoDia, chuva)
            .Where(c => c.CentroLat >= lat - deltaLat && c.CentroLat <= lat + deltaLat &&
                c.CentroLon >= lon - deltaLon && c.CentroLon <= lon + deltaLon)
            .ToListAsync(ct);
        return candidatos.Where(c => Posicao.DistanciaMetros(lat, lon, c.CentroLat, c.CentroLon) <= raio)
            .OrderByDescending(c => c.Score).ThenBy(c => c.CelulaId).Take(500).ToArray();
    }

    public async Task<IReadOnlyList<CelulaRisco>> DetalheCelulaAsync(string id, CancellationToken ct)
        => await context.CelulasRisco.AsNoTracking().Where(c => c.CelulaId == id)
            .OrderBy(c => c.FaixaHoraria).ThenBy(c => c.TipoDia).ThenBy(c => c.Chuva).ToListAsync(ct);

    public async Task<IReadOnlyList<CelulaRisco>> RiscoPorDistritosAsync(int[] distritos, int faixa,
        string tipoDia, bool chuva, CancellationToken ct)
        => await Contexto(faixa, tipoDia, chuva).Where(c => distritos.Contains(c.DistritoId))
            .OrderByDescending(c => c.Score).ThenBy(c => c.CelulaId).Take(500).ToListAsync(ct);

    private IQueryable<CelulaRisco> Contexto(int faixa, string tipoDia, bool chuva)
        => context.CelulasRisco.AsNoTracking().Where(c => c.FaixaHoraria == faixa &&
            c.TipoDia == tipoDia && c.Chuva == chuva && c.Percentil >= 95);
}
