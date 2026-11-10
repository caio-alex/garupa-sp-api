using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garupa.Domain.Entities;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class ConteudoTests
{
    [Fact]
    public async Task PerfilValidaDistritosAtualizaERevoga()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var distritos = await client.GetFromJsonAsync<JsonElement[]>("/api/v1/distritos");
        Assert.Equal(96, distritos!.Length);
        var id = await Cenarios.CadastrarAsync(client);
        Assert.Equal(HttpStatusCode.Conflict, (await Cenarios.PerfilAsync(client, id, "lazer", [355030801])).StatusCode);
        await Cenarios.ConsentirAsync(client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await Cenarios.PerfilAsync(client, id, "lazer", [355030801, 355030802])).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Cenarios.PerfilAsync(client, id, "ida_volta", [355030802, 355030803])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cenarios.PerfilAsync(client, id, "invalido", [])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cenarios.PerfilAsync(client, id, "lazer", [1])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cenarios.PerfilAsync(client, id, "lazer", [355030801, 355030801])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cenarios.PerfilAsync(client, id, "lazer", [355030801, 355030802, 355030803, 355030804, 355030805, 355030806])).StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        Assert.Equal(new[] { 355030802, 355030803 }, await context.Set<DistritoFrequente>().OrderBy(d => d.DistritoId).Select(d => d.DistritoId).ToArrayAsync());
        await Cenarios.ConsentirAsync(client, id, perfil: false);
        Assert.Empty(await context.Set<DistritoFrequente>().ToListAsync());
        Assert.Empty(await context.Perfis.ToListAsync());
    }

    [Fact]
    public async Task MensagensFiltramContextoEPerfilComFontesOficiais()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var comuns = await client.GetFromJsonAsync<JsonElement[]>("/api/v1/mensagens?contexto=chuva&tipoDeUso=lazer");
        var entregador = await client.GetFromJsonAsync<JsonElement[]>("/api/v1/mensagens?contexto=chuva&tipoDeUso=entregador_app");
        Assert.Single(comuns!);
        Assert.Equal(2, entregador!.Length);
        Assert.All(entregador, m =>
        {
            Assert.True(m.GetProperty("texto").GetString()!.Length <= 140);
            Assert.StartsWith("https://www.gov.br/", m.GetProperty("fonte").GetString());
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/mensagens?contexto=invalido")).StatusCode);
    }

    [Fact]
    public async Task RiscoEPlanoFiltramContextoRaioPercentilEDistritoSemAlterarScore()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var usuario = await Cenarios.CadastrarAsync(client);
        await Cenarios.ConsentirAsync(client, usuario);
        await Cenarios.PerfilAsync(client, usuario, "entregador_app", [355030801]);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
            context.CelulasRisco.AddRange(
                Celula("a", -23.55, -46.63, 0.9, 99, 355030801),
                Celula("b", -23.551, -46.63, 0.8, 96, 355030802),
                Celula("c", -23.55, -46.63, 0.7, 94, 355030801),
                Celula("d", -23.75, -46.63, 0.6, 99, 355030801));
            await context.SaveChangesAsync();
        }
        var risco = await client.GetFromJsonAsync<JsonElement[]>("/api/v1/risco?lat=-23.55&lon=-46.63&raioMetros=500&faixaHoraria=2&tipoDia=util&chuva=true");
        Assert.Equal(new[] { "a", "b" }, risco!.Select(c => c.GetProperty("celulaId").GetString()).ToArray());
        var plano = await client.GetFromJsonAsync<JsonElement>($"/api/v1/plano-do-turno?usuarioId={usuario}&faixaHoraria=2&tipoDia=util&chuva=true");
        Assert.Equal(new[] { "a", "d" }, plano.GetProperty("pontosCriticos").EnumerateArray().Select(c => c.GetProperty("celulaId").GetString()).ToArray());
        Assert.Equal(0.9, plano.GetProperty("pontosCriticos")[0].GetProperty("score").GetDouble());
        var mensagens = plano.GetProperty("mensagens").EnumerateArray().Select(m => m.GetProperty("id").GetInt32()).ToArray();
        Assert.Contains(4, mensagens);
        await Cenarios.PerfilAsync(client, usuario, "lazer", [355030801]);
        plano = await client.GetFromJsonAsync<JsonElement>($"/api/v1/plano-do-turno?usuarioId={usuario}&faixaHoraria=2&tipoDia=util&chuva=true");
        Assert.DoesNotContain(4, plano.GetProperty("mensagens").EnumerateArray().Select(m => m.GetProperty("id").GetInt32()));
        Assert.Equal(0.9, plano.GetProperty("pontosCriticos")[0].GetProperty("score").GetDouble());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/risco/celulas/ausente")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/risco/celulas/a")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/risco?lat=-23.55")).StatusCode);
    }

    [Fact]
    public async Task PlanoSemPerfilContinuaDisponivel()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var usuario = await Cenarios.CadastrarAsync(client);
        var plano = await client.GetFromJsonAsync<JsonElement>($"/api/v1/plano-do-turno?usuarioId={usuario}&faixaHoraria=2&tipoDia=util&chuva=false");
        Assert.Equal(0, plano.GetProperty("pontosCriticos").GetArrayLength());
        Assert.True(plano.GetProperty("mensagens").GetArrayLength() > 0);
    }

    private static CelulaRisco Celula(string id, double lat, double lon, double score, int percentil, int distrito)
        => new(id, lat, lon, 2, "util", true, score, percentil, "Via fictícia", distrito, "teste");

    [Fact]
    public async Task PlanoUsaHorarioHabitualQuandoFaixaForOmitidaEConsultaTemPrioridade()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await Cenarios.ConsentirAsync(client, id);
        await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/perfil", new
        {
            tiposDeUso = new[] { "lazer" },
            horariosHabituais = new[] { new { faixaHoraria = 4, tipoDia = "util" }, new { faixaHoraria = 1, tipoDia = "sabado" } },
            distritosFrequentes = Array.Empty<int>()
        });
        var habitual = await client.GetFromJsonAsync<JsonElement>($"/api/v1/plano-do-turno?usuarioId={id}&tipoDia=util&chuva=false");
        Assert.Equal(4, habitual.GetProperty("faixaHoraria").GetInt32());
        Assert.Equal("perfil", habitual.GetProperty("origemFaixa").GetString());
        var explicito = await client.GetFromJsonAsync<JsonElement>($"/api/v1/plano-do-turno?usuarioId={id}&faixaHoraria=2&tipoDia=util&chuva=false");
        Assert.Equal(2, explicito.GetProperty("faixaHoraria").GetInt32());
        Assert.Equal("consulta", explicito.GetProperty("origemFaixa").GetString());
        var semHabito = await client.GetFromJsonAsync<JsonElement>($"/api/v1/plano-do-turno?usuarioId={id}&tipoDia=domingo&chuva=false");
        Assert.Equal(2, semHabito.GetProperty("faixaHoraria").GetInt32());
        Assert.Equal("horario_atual", semHabito.GetProperty("origemFaixa").GetString());
    }

    [Fact]
    public async Task RespostasRespeitamOrcamentoComTextosUnicodeLongos()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var usuario = await Cenarios.CadastrarAsync(client);
        await Cenarios.ConsentirAsync(client, usuario);
        await Cenarios.PerfilAsync(client, usuario, "lazer", [355030801]);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
            for (var i = 0; i < 500; i++)
                context.CelulasRisco.Add(new CelulaRisco(new string('Á', 90) + i, -23.55, -46.63,
                    2, "util", false, 0.99, 99, new string('Á', 200), 355030801, new string('Á', 100)));
            await context.SaveChangesAsync();
        }
        var risco = await client.GetByteArrayAsync("/api/v1/risco?lat=-23.55&lon=-46.63&raioMetros=500&faixaHoraria=2&tipoDia=util&chuva=false");
        var plano = await client.GetByteArrayAsync($"/api/v1/plano-do-turno?usuarioId={usuario}&faixaHoraria=2&tipoDia=util&chuva=false");
        Assert.True(risco.Length <= 200000);
        Assert.True(plano.Length <= 200000);
        Assert.InRange(JsonSerializer.Deserialize<JsonElement[]>(risco)!.Length, 1, 499);
    }

    [Theory]
    [InlineData("geral")]
    [InlineData("chuva")]
    [InlineData("noite")]
    [InlineData("domingo")]
    [InlineData("ponto_critico")]
    public async Task CatalogoOfereceMensagemGeralParaCadaContexto(string contexto)
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var mensagens = await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/mensagens?contexto={contexto}&tipoDeUso=ida_volta");
        Assert.NotEmpty(mensagens!);
    }
}
