
using Garupa.Domain.Entities;
using Garupa.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Garupa.Infrastructure.Data;

public class GarupaDbContext : DbContext
{
    private readonly ICriptografiaDados criptografia;
    public GarupaDbContext(
        DbContextOptions<GarupaDbContext> options, ICriptografiaDados criptografia)
        : base(options)
    {
        this.criptografia = criptografia;
    }

    public DbSet<Sos> Sos => Set<Sos>();
    public DbSet<Consentimento> Consentimentos => Set<Consentimento>();
    public DbSet<PerfilUso> Perfis => Set<PerfilUso>();
    public DbSet<Distrito> Distritos => Set<Distrito>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<CelulaRisco> CelulasRisco => Set<CelulaRisco>();
    public DbSet<Vestivel> Vestiveis => Set<Vestivel>();
    public DbSet<EventoVestivel> EventosVestivel => Set<EventoVestivel>();
    public DbSet<AlertaVestivel> AlertasVestivel => Set<AlertaVestivel>();
    public DbSet<Relato> Relatos => Set<Relato>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<ContatoEmergencia> ContatosEmergencia
        => Set<ContatoEmergencia>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        RecursosModel.Configurar(modelBuilder, criptografia);

        modelBuilder.Entity<Sos>(entity =>
        {
            entity.ToTable("sos");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.EventoId).HasMaxLength(36).IsRequired();
            entity.Property(s => s.TokenAcompanhamento).IsRequired();
            entity.Property(s => s.Origem).HasMaxLength(10).IsRequired();
            entity.HasIndex(s => s.EventoId).IsUnique();
            entity.HasIndex(s => s.TokenHash).IsUnique();
            entity.Property(s => s.Estado).IsConcurrencyToken();
            entity.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Versao).IsConcurrencyToken();

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
