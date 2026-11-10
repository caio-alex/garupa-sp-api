using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Garupa.Infrastructure.Data;

internal static class RecursosModel
{
    public static void Configurar(ModelBuilder model, ICriptografiaDados criptografia)
    {
        var converter = new ValueConverter<string, string>(v => criptografia.Proteger(v), v => criptografia.Desproteger(v));
        model.Entity<Sos>().Property(s => s.TokenAcompanhamento).HasConversion(converter);
        model.Entity<Sos>().Property(s => s.TokenHash).HasMaxLength(64);
        model.Entity<FichaMedica>(e =>
        {
            e.Property(f => f.TipoSanguineo).HasConversion(converter);
            e.Property(f => f.Alergias).HasConversion(converter);
            e.Property(f => f.Condicoes).HasConversion(converter);
            e.Property(f => f.Medicamentos).HasConversion(converter);
        });
        model.Entity<Consentimento>(e =>
        {
            e.ToTable("consentimentos");
            e.HasKey(c => c.UsuarioId);
            e.Property(c => c.UsuarioId).ValueGeneratedNever();
            e.Property(c => c.AtualizadoEm).IsConcurrencyToken();
            e.HasOne<Usuario>().WithOne().HasForeignKey<Consentimento>(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<Distrito>(e =>
        {
            e.ToTable("distritos");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedNever();
            e.Property(d => d.Nome).HasMaxLength(100).IsRequired();
            e.HasData(CatalogoInicial.Distritos());
        });
        model.Entity<PerfilUso>(e =>
        {
            e.ToTable("perfis_uso");
            e.HasKey(p => p.UsuarioId);
            e.Property(p => p.UsuarioId).ValueGeneratedNever();
            e.HasOne<Usuario>().WithOne().HasForeignKey<PerfilUso>(p => p.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Distritos).WithOne().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<DistritoFrequente>(e =>
        {
            e.ToTable("distritos_frequentes");
            e.HasKey(d => new { d.UsuarioId, d.DistritoId });
            e.HasOne<Distrito>().WithMany().HasForeignKey(d => d.DistritoId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Mensagem>(e =>
        {
            e.ToTable("mensagens");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).ValueGeneratedNever();
            e.Property(m => m.Texto).HasMaxLength(140).IsRequired();
            e.Property(m => m.Contexto).HasMaxLength(20).IsRequired();
            e.Property(m => m.TipoDeUso).HasMaxLength(30);
            e.Property(m => m.Fonte).HasMaxLength(500).IsRequired();
            e.HasData(CatalogoInicial.Mensagens());
        });
        model.Entity<CelulaRisco>(e =>
        {
            e.ToTable("celulas_risco");
            e.HasKey(c => c.Id);
            e.Property(c => c.CelulaId).HasMaxLength(100).IsRequired();
            e.Property(c => c.TipoDia).HasMaxLength(10).IsRequired();
            e.Property(c => c.ViaReferencia).HasMaxLength(200).IsRequired();
            e.Property(c => c.VersaoModelo).HasMaxLength(100).IsRequired();
            e.HasIndex(c => new { c.CelulaId, c.FaixaHoraria, c.TipoDia, c.Chuva }).IsUnique();
            e.HasIndex(c => new { c.DistritoId, c.FaixaHoraria, c.TipoDia, c.Chuva, c.Percentil });
            e.HasOne<Distrito>().WithMany().HasForeignKey(c => c.DistritoId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Vestivel>(e =>
        {
            e.ToTable("vestiveis");
            e.HasKey(v => v.UsuarioId);
            e.Property(v => v.UsuarioId).ValueGeneratedNever();
            e.Property(v => v.DispositivoId).HasMaxLength(100).IsRequired();
            e.HasIndex(v => v.DispositivoId).IsUnique();
            e.HasAlternateKey(v => new { v.UsuarioId, v.DispositivoId });
            e.HasOne<Usuario>().WithOne().HasForeignKey<Vestivel>(v => v.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<EventoVestivel>(e =>
        {
            e.ToTable("eventos_vestivel");
            e.HasKey(v => v.Id);
            e.Property(v => v.DispositivoId).HasMaxLength(100).IsRequired();
            e.Property(v => v.Tipo).HasMaxLength(30).IsRequired();
            e.HasIndex(v => new { v.UsuarioId, v.ConsumidoEm });
            e.HasOne<Vestivel>().WithMany().HasForeignKey(v => new { v.UsuarioId, v.DispositivoId })
                .HasPrincipalKey(v => new { v.UsuarioId, v.DispositivoId }).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<AlertaVestivel>(e =>
        {
            e.ToTable("alertas_vestivel");
            e.HasKey(v => v.Id);
            e.Property(v => v.DispositivoId).HasMaxLength(100).IsRequired();
            e.Property(v => v.Tipo).HasMaxLength(30).IsRequired();
            e.HasIndex(v => new { v.DispositivoId, v.ConsumidoEm, v.OcorridoEm });
            e.HasOne<Vestivel>().WithMany().HasForeignKey(v => new { v.UsuarioId, v.DispositivoId })
                .HasPrincipalKey(v => new { v.UsuarioId, v.DispositivoId }).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<Relato>(e =>
        {
            e.ToTable("relatos");
            e.HasKey(r => r.Id);
            e.Property(r => r.EventoId).HasMaxLength(36).IsRequired();
            e.HasIndex(r => r.EventoId).IsUnique();
            e.Property(r => r.Tipo).HasMaxLength(40).IsRequired();
            e.Property(r => r.Descricao).HasMaxLength(280).IsRequired();
            e.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
