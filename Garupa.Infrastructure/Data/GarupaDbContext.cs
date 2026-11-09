
using Garupa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Data;

public class GarupaDbContext : DbContext
{
    public GarupaDbContext(
        DbContextOptions<GarupaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<ContatoEmergencia> ContatosEmergencia
        => Set<ContatoEmergencia>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // A ficha médica será mapeada em uma etapa futura,
        // com criptografia em repouso.
        // modelBuilder.Ignore<FichaMedica>();

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(u => u.Telefone)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasMany(u => u.Contatos)
                .WithOne()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(u => u.Contatos)
                .UsePropertyAccessMode(
                    PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ContatoEmergencia>(entity =>
        {
            entity.ToTable("contatos_emergencia");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(c => c.Telefone)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(c => c.UsuarioId);
        });
    }
}
