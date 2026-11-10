using Garupa.Domain.Entities;
using Garupa.Domain.Enums;
using Garupa.Domain.Exceptions;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class DominioTests
{
    private static readonly DateTime Agora = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("+55 (11) 98765-4321", "11987654321")]
    [InlineData("(11) 3456-7890", "1134567890")]
    [InlineData("11987654321", "11987654321")]
    public void NormalizaTelefone(string telefone, string esperado)
        => Assert.Equal(esperado, TelefoneBrasileiro.Validar(telefone));

    [Theory]
    [InlineData("123")]
    [InlineData("11987654321abc")]
    [InlineData("00987654321")]
    [InlineData("")]
    public void RecusaTelefoneInvalido(string telefone)
        => Assert.Throws<ArgumentException>(() => TelefoneBrasileiro.Validar(telefone));

    [Fact]
    public void EncerramentoAutomaticoConsideraPrazoMesmoSemConsultaAnterior()
    {
        var sos = NovoSos();
        sos.AtualizarExpiracao(Agora.AddHours(25));
        Assert.Equal(EstadoSos.Encerrado, sos.Estado);
        Assert.Equal(Agora.AddHours(12), sos.TerminadoEm);
        Assert.True(sos.TokenExpirou(Agora.AddHours(24)));
        Assert.Throws<ConflitoSosException>(() => sos.Cancelar(Agora.AddHours(25)));
    }

    [Fact]
    public void DataTemPrecisaoCompativelComPostgres()
    {
        var ocorridoEm = Agora.AddTicks(-1);
        var sos = new Sos(Guid.NewGuid().ToString(), 1, TipoEmergencia.Queda,
            "celular", -23.55, -46.63, ocorridoEm, Agora);
        Assert.Equal(0, sos.OcorridoEm.Ticks % 10);
    }

    [Fact]
    public async Task BancoRejeitaDuplicacaoDiretaDoEvento()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        var usuario = new Usuario("Pessoa fictícia", "11987654321");
        usuario.AdicionarContato(new ContatoEmergencia("Contato fictício", "1134567890"));
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        var eventoId = Guid.NewGuid().ToString();
        context.Sos.Add(new Sos(eventoId, usuario.Id, TipoEmergencia.Queda,
            "celular", -23.55, -46.63, Agora, Agora));
        await context.SaveChangesAsync();
        context.Sos.Add(new Sos(eventoId, usuario.Id, TipoEmergencia.Queda,
            "celular", -23.55, -46.63, Agora, Agora));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static Sos NovoSos() => new(Guid.NewGuid().ToString(), 1,
        TipoEmergencia.Queda, "celular", -23.55, -46.63, Agora, Agora);
}
