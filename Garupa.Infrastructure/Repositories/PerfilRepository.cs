using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Repositories;

public class PerfilRepository(GarupaDbContext context) : IPerfilRepository
{
    public Task<PerfilUso?> ObterAsync(int usuarioId, CancellationToken ct)
        => context.Perfis.Include(p => p.Distritos).SingleOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

    public async Task<IReadOnlyList<Distrito>> ListarDistritosAsync(CancellationToken ct)
        => await context.Distritos.AsNoTracking().OrderBy(d => d.Nome).ToListAsync(ct);

    public async Task<bool> DistritosExistemAsync(int[] ids, CancellationToken ct)
        => await context.Distritos.CountAsync(d => ids.Contains(d.Id), ct) == ids.Length;

    public async Task SalvarAsync(PerfilUso perfil, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        if ((await BloqueioConsentimento.ObterAsync(context, perfil.UsuarioId, ct))?.PerfilDeUso != true)
            throw new Garupa.Domain.Exceptions.ConflitoRegraException("Consentimento revogado.");
        if (context.Entry(perfil).State == EntityState.Detached) context.Perfis.Add(perfil);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
