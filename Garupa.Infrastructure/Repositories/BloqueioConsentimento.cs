using Garupa.Domain.Entities;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Repositories;

internal static class BloqueioConsentimento
{
    // Os chamadores mantêm a transação aberta até gravar os dados opcionais ou a revogação.
    public static Task<Consentimento?> ObterAsync(GarupaDbContext context, int usuarioId, CancellationToken ct)
        => context.Database.IsNpgsql()
            ? context.Consentimentos.FromSqlInterpolated($"SELECT * FROM consentimentos WHERE \"UsuarioId\" = {usuarioId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(ct)
            : context.Consentimentos.AsNoTracking().SingleOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);
}
