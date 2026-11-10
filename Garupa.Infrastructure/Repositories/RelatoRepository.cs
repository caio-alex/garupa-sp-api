using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Garupa.Infrastructure.Repositories;

public class RelatoRepository(GarupaDbContext context) : IRelatoRepository
{
    public Task<Relato?> ObterPorEventoAsync(string eventoId, CancellationToken ct)
        => context.Relatos.SingleOrDefaultAsync(r => r.EventoId == eventoId, ct);
    public async Task<Relato> AdicionarAsync(Relato relato, CancellationToken ct)
    {
        context.Relatos.Add(relato);
        try
        {
            await context.SaveChangesAsync(ct);
            return relato;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_relatos_EventoId" })
        {
            context.Entry(relato).State = EntityState.Detached;
            return await context.Relatos.SingleAsync(r => r.EventoId == relato.EventoId, ct);
        }
    }
}
