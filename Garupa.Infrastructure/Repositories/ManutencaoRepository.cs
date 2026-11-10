using Garupa.Domain.Enums;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Repositories;

public class ManutencaoRepository(GarupaDbContext context) : IManutencaoRepository
{
    public async Task ExecutarAsync(DateTime agora, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var limiteSos = agora.AddHours(-12);
        await context.Sos.Where(s => s.Estado == EstadoSos.Ativo && s.CriadoEm <= limiteSos)
            .ExecuteUpdateAsync(update => update.SetProperty(s => s.Estado, EstadoSos.Encerrado)
                .SetProperty(s => s.TerminadoEm, s => s.CriadoEm.AddHours(12)), ct);
        var limitePosicao = agora.AddDays(-30);
        await context.Sos.Where(s => s.CriadoEm <= limitePosicao && s.Latitude != null)
            .ExecuteUpdateAsync(update => update.SetProperty(s => s.Latitude, (double?)null)
                .SetProperty(s => s.Longitude, (double?)null), ct);
        var limiteAlertas = agora.AddSeconds(-30);
        await context.AlertasVestivel.Where(a => a.OcorridoEm <= limiteAlertas).ExecuteDeleteAsync(ct);
        var limiteEventos = agora.AddDays(-1);
        await context.EventosVestivel.Where(e => e.ConsumidoEm != null && e.ConsumidoEm <= limiteEventos).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
