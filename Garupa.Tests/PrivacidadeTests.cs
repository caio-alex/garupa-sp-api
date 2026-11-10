using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Garupa.Application.Interfaces;
using Garupa.Domain.Entities;
using Garupa.Infrastructure.Data;
using Garupa.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class PrivacidadeTests
{
    [Fact]
    public async Task FichaExigeConsentimentoEFicaCriptografadaNoBanco()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        var ficha = new { tipoSanguineo = "O+", alergias = "Alergia fictícia", condicoes = "", medicamentos = "" };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/ficha-medica", ficha)).StatusCode);
        await Cenarios.ConsentirAsync(client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/ficha-medica", ficha)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        await context.Database.OpenConnectionAsync();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT \"Alergias\" FROM \"FichaMedica\"";
        var protegido = (string)(await command.ExecuteScalarAsync())!;
        Assert.StartsWith("g1:", protegido);
        Assert.DoesNotContain("fictícia", protegido);
        Assert.Equal(ficha.alergias, scope.ServiceProvider.GetRequiredService<ICriptografiaDados>().Desproteger(protegido));
        var armazenada = await context.Set<FichaMedica>().AsNoTracking().SingleAsync();
        Assert.Equal(ficha.alergias, armazenada.Alergias);
        Assert.DoesNotContain("fichaMedica", await client.GetStringAsync($"/api/v1/usuarios/{id}"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/ficha-medica", new { })).StatusCode);
        Assert.Equal(1, await context.Set<FichaMedica>().CountAsync());
        await Cenarios.ConsentirAsync(client, id, ficha: false);
        Assert.Equal(0, await context.Set<FichaMedica>().CountAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/ficha-medica", ficha)).StatusCode);
    }

    [Fact]
    public async Task ConversaoDeFichasLegadasPreservaDadosEPodeSerRepetida()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"FichaMedica\" (\"UsuarioId\", \"TipoSanguineo\", \"Alergias\", \"Condicoes\", \"Medicamentos\") VALUES ({id}, {"O+"}, {"Legado fictício"}, {""}, {""})");
        var criptografia = scope.ServiceProvider.GetRequiredService<ICriptografiaDados>();
        await PreparacaoBanco.ProtegerFichasAntigasAsync(context, criptografia);
        await PreparacaoBanco.ProtegerFichasAntigasAsync(context, criptografia);
        Assert.Equal("Legado fictício", (await context.Set<FichaMedica>().AsNoTracking().SingleAsync()).Alergias);
    }

    [Fact]
    public async Task ExcluirUsuarioRemoveDadosDependentesEPreservaCatalogos()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await Cenarios.ConsentirAsync(client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await Cenarios.PerfilAsync(client, id, "lazer", [355030801])).StatusCode);
        await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/ficha-medica", new { tipoSanguineo = "AB-" });
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/vestivel", new { dispositivoId = "teste" });
        await client.PostAsJsonAsync("/api/v1/eventos-vestivel", new { dispositivoId = "teste", tipo = "sos", ocorridoEm = factory.Clock.Agora });
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/alertas-vestivel", new { tipo = "sos_enviado", ocorridoEm = factory.Clock.Agora });
        await client.PostAsJsonAsync("/api/v1/relatos", new { eventoId = Guid.NewGuid(), usuarioId = id, tipo = "buraco", lat = -23.55, lon = -46.63, descricao = "Fictício" });
        await client.PostAsJsonAsync("/api/v1/sos", new { eventoId = Guid.NewGuid(), usuarioId = id, tipo = "queda", origem = "celular", ocorridoEm = factory.Clock.Agora, lat = -23.55, lon = -46.63 });
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/usuarios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/usuarios/{id}")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        Assert.Empty(await context.Usuarios.ToListAsync());
        Assert.Empty(await context.ContatosEmergencia.ToListAsync());
        Assert.Empty(await context.Set<FichaMedica>().ToListAsync());
        Assert.Empty(await context.Consentimentos.ToListAsync());
        Assert.Empty(await context.Perfis.ToListAsync());
        Assert.Empty(await context.Set<DistritoFrequente>().ToListAsync());
        Assert.Empty(await context.Sos.ToListAsync());
        Assert.Empty(await context.Relatos.ToListAsync());
        Assert.Empty(await context.Vestiveis.ToListAsync());
        Assert.Empty(await context.EventosVestivel.ToListAsync());
        Assert.Empty(await context.AlertasVestivel.ToListAsync());
        Assert.Equal(96, await context.Distritos.CountAsync());
        Assert.True(await context.Mensagens.AnyAsync());
    }

    [Fact]
    public async Task ConsentimentosExigemTodosOsCamposEValidamDependencia()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/consentimentos", new { fichaMedica = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/consentimentos", new
        {
            localizacaoEmUso = false, localizacaoEmSegundoPlano = true, fichaMedica = false,
            historicoDeLocais = false, perfilDeUso = false
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/v1/usuarios/999/ficha-medica", new { })).StatusCode);
    }

    [Fact]
    public void CriptografiaUsaNonceAleatorioERecusaAlteracaoOuChaveErrada()
    {
        var cipher = new CriptografiaDados(RandomNumberGenerator.GetBytes(32));
        var a = cipher.Proteger("Dado fictício");
        var b = cipher.Proteger("Dado fictício");
        Assert.NotEqual(a, b);
        Assert.Equal("Dado fictício", cipher.Desproteger(a));
        Assert.ThrowsAny<CryptographicException>(() => new CriptografiaDados(RandomNumberGenerator.GetBytes(32)).Desproteger(a));
        var bytes = Convert.FromBase64String(a[3..]);
        bytes[^1] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => cipher.Desproteger("g1:" + Convert.ToBase64String(bytes)));
    }
}
