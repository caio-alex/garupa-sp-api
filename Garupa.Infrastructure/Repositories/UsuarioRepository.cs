
using Garupa.Domain.Entities;
using Garupa.Domain.Repositories;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly GarupaDbContext _context;

    public UsuarioRepository(GarupaDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObterPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .Include(u => u.Contatos)
            .FirstOrDefaultAsync(
                u => u.Id == id,
                cancellationToken);
    }

    public async Task AdicionarAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(
            usuario,
            cancellationToken);
    }

    public async Task SalvarAlteracoesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
