using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class VestivelERelatoTests
{
    [Fact]
    public async Task PareamentoEventosEAlertasSaoPersistidosEConsumidosUmaVez()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        var outro = await Cenarios.CadastrarAsync(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/v1/eventos-vestivel", Evento(factory))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/vestivel", new { dispositivoId = "teste" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/vestivel", new { dispositivoId = "teste" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/v1/usuarios/{outro}/vestivel", new { dispositivoId = "teste" })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/eventos-vestivel", Evento(factory))).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/eventos-vestivel/pendentes?usuarioId={id}"))!);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/eventos-vestivel/pendentes?usuarioId={id}"))!);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/alertas-vestivel", Alerta(factory))).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/v1/vestivel/teste/alertas/pendentes"))!);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/v1/vestivel/teste/alertas/pendentes"))!);
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/alertas-vestivel", Alerta(factory));
        factory.Clock.Agora = factory.Clock.Agora.AddSeconds(30);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/v1/vestivel/teste/alertas/pendentes"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/usuarios/{id}/vestivel")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/vestivel/teste/alertas/pendentes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/alertas-vestivel", Alerta(factory))).StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        Assert.Empty(await context.EventosVestivel.ToListAsync());
        Assert.Empty(await context.AlertasVestivel.ToListAsync());
    }

    [Fact]
    public async Task NovoPareamentoDescartaPendenciasDoDispositivoAnterior()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/vestivel", new { dispositivoId = "teste" });
        await client.PostAsJsonAsync("/api/v1/eventos-vestivel", Evento(factory));
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/alertas-vestivel", Alerta(factory));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/vestivel", new { dispositivoId = "novo" })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>($"/api/v1/eventos-vestivel/pendentes?usuarioId={id}"))!);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>("/api/v1/vestivel/novo/alertas/pendentes"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/v1/eventos-vestivel", Evento(factory))).StatusCode);
    }

    [Fact]
    public async Task RelatoValidaPoligonoEReenvio()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        var evento = Guid.NewGuid();
        var body = new { usuarioId = id, eventoId = evento, tipo = "buraco", lat = -23.55, lon = -46.63, descricao = "Fictício" };
        var a = await client.PostAsJsonAsync("/api/v1/relatos", body);
        var b = await client.PostAsJsonAsync("/api/v1/relatos", body);
        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/relatos", new
        {
            usuarioId = id, eventoId = evento, tipo = "buraco", lat = -23.55, lon = -46.63, descricao = "Outro"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/relatos", new
        {
            usuarioId = id, eventoId = Guid.NewGuid(), tipo = "buraco", lat = -23.5325, lon = -46.7917, descricao = "Osasco fictício"
        })).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<GarupaDbContext>().Relatos.CountAsync());
    }

    [PostgresFact]
    public async Task ConsultasSimultaneasNaoReentregamAlertasOuEventos()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/vestivel", new { dispositivoId = "teste" });
        await client.PostAsJsonAsync("/api/v1/eventos-vestivel", Evento(factory));
        await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/alertas-vestivel", Alerta(factory));
        var eventos = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.GetFromJsonAsync<JsonElement[]>($"/api/v1/eventos-vestivel/pendentes?usuarioId={id}")));
        var alertas = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.GetFromJsonAsync<JsonElement[]>("/api/v1/vestivel/teste/alertas/pendentes")));
        Assert.Equal(1, eventos.Sum(e => e!.Length));
        Assert.Equal(1, alertas.Sum(a => a!.Length));
    }

    [PostgresFact]
    public async Task RelatosConcorrentesNaoDuplicam()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        var body = new { usuarioId = id, eventoId = Guid.NewGuid(), tipo = "buraco", lat = -23.55, lon = -46.63, descricao = "Fictício" };
        var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync("/api/v1/relatos", body)));
        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var ids = await Task.WhenAll(respostas.Select(r => r.Content.ReadAsStringAsync()));
        Assert.Single(ids.Distinct());
    }

    private static object Evento(ApiFactory factory) => new { dispositivoId = "teste", tipo = "impacto", ocorridoEm = factory.Clock.Agora };
    private static object Alerta(ApiFactory factory) => new { tipo = "ponto_critico", ocorridoEm = factory.Clock.Agora };
}
