using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Garupa.Infrastructure.Repositories;

public class SosRepository(GarupaDbContext context) : ISosRepository
{
    public Task<Sos?> ObterPorEventoAsync(string eventoId, CancellationToken cancellationToken)
        => context.Sos.SingleOrDefaultAsync(s => s.EventoId == eventoId, cancellationToken);
    public Task<Sos?> ObterPorIdAsync(int id, CancellationToken cancellationToken)
        => context.Sos.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    public Task<Sos?> ObterPorTokenAsync(string token, CancellationToken cancellationToken)
    {
        var hash = Sos.CalcularHash(token);
        return context.Sos.SingleOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);
    }

    public async Task<Sos> AdicionarAsync(Sos sos, CancellationToken cancellationToken)
    {
        context.Sos.Add(sos);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return sos;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_sos_EventoId" })
        {
            context.Entry(sos).State = EntityState.Detached;
            return await context.Sos.SingleAsync(s => s.EventoId == sos.EventoId, cancellationToken);
        }
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
        => await context.SaveChangesAsync(cancellationToken);
}
