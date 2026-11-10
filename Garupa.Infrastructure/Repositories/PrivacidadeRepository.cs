using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Repositories;

public class PrivacidadeRepository(GarupaDbContext context) : IPrivacidadeRepository
{
    public Task<Consentimento?> ObterConsentimentoAsync(int usuarioId, CancellationToken ct)
        => context.Consentimentos.SingleOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

    public async Task SalvarConsentimentoAsync(Consentimento consentimento, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await BloqueioConsentimento.ObterAsync(context, consentimento.UsuarioId, ct);
        if (context.Entry(consentimento).State == EntityState.Detached)
            context.Consentimentos.Add(consentimento);
        // A revogação elimina os dados opcionais na mesma transação da atualização.
        if (!consentimento.FichaMedica)
            context.RemoveRange(await context.Set<FichaMedica>().Where(f => f.UsuarioId == consentimento.UsuarioId).ToListAsync(ct));
        if (!consentimento.PerfilDeUso)
            context.RemoveRange(await context.Perfis.Where(p => p.UsuarioId == consentimento.UsuarioId).ToListAsync(ct));
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task SalvarFichaAsync(int usuarioId, FichaMedica ficha, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        if ((await BloqueioConsentimento.ObterAsync(context, usuarioId, ct))?.FichaMedica != true)
            throw new Garupa.Domain.Exceptions.ConflitoRegraException("Consentimento revogado.");
        var usuario = await context.Usuarios.Include(u => u.FichaMedica).SingleAsync(u => u.Id == usuarioId, ct);
        if (usuario.FichaMedica is null) usuario.AtualizarFichaMedica(ficha);
        else usuario.FichaMedica.Atualizar(ficha.TipoSanguineo, ficha.Alergias, ficha.Condicoes, ficha.Medicamentos);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<bool> ExcluirUsuarioAsync(int usuarioId, CancellationToken ct)
        => await context.Usuarios.Where(u => u.Id == usuarioId).ExecuteDeleteAsync(ct) > 0;
}
