using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Garupa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Garupa.Tests;

public class FluxoApiTests
{
    private static object Cadastro(string telefone = "11987654321") => new
    {
        nome = "Pessoa ficticia", telefone,
        contato = new { nome = "Contato ficticio", telefone = "1134567890" }
    };

    private static object Evento(int usuarioId, string eventoId, DateTimeOffset agora) => new
    {
        eventoId, usuarioId, tipo = "queda", origem = "celular", ocorridoEm = agora,
        lat = -23.55, lon = -46.63
    };

    private static async Task<int> Cadastrar(HttpClient client)
    {
        var resposta = await client.PostAsJsonAsync("/api/v1/usuarios", Cadastro());
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetInt32();
    }

    [Fact]
    public async Task CadastroPersisteContatoNaMesmaOperacao()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cadastrar(client);
        var usuario = await client.GetFromJsonAsync<JsonElement>($"/api/v1/usuarios/{id}");
        Assert.Single(usuario.GetProperty("contatos").EnumerateArray());
        var contato = await client.PostAsJsonAsync($"/api/v1/usuarios/{id}/contatos",
            new { nome = "Outro ficticio", telefone = "21987654321" });
        Assert.Equal(HttpStatusCode.Created, contato.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/usuarios/999")).StatusCode);
    }

    [Fact]
    public async Task CadastroSemContatoOuComTelefoneInvalidoNaoGravaNada()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var semContato = await client.PostAsJsonAsync("/api/v1/usuarios", new { nome = "Teste", telefone = "11987654321" });
        Assert.Equal(HttpStatusCode.BadRequest, semContato.StatusCode);
        var invalido = await client.PostAsJsonAsync("/api/v1/usuarios", Cadastro("fone_invalido_xyz"));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.DoesNotContain("fone_invalido_xyz", await invalido.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GarupaDbContext>();
        Assert.Equal(0, await context.Usuarios.CountAsync());
        Assert.Equal(0, await context.ContatosEmergencia.CountAsync());
    }

    [Theory]
    [InlineData("cancelar", "cancelado")]
    [InlineData("encerrar", "encerrado")]
    public async Task SosPersisteReenvioNaoDuplicaETransicaoPodeSerRepetida(string acao, string estado)
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var usuarioId = await Cadastrar(client);
        var eventoId = Guid.NewGuid().ToString();
        var body = Evento(usuarioId, eventoId, factory.Clock.Agora);
        var resposta = await client.PostAsJsonAsync("/api/v1/sos", body);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var repetido = await client.PostAsJsonAsync("/api/v1/sos", body);
        var segundo = await repetido.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(criado.ToString(), segundo.ToString());
        var token = criado.GetProperty("tokenAcompanhamento").GetString();
        var id = criado.GetProperty("sosId").GetInt32();
        var acompanhamento = await client.GetFromJsonAsync<JsonElement>($"/api/v1/acompanhar/{token}");
        Assert.Equal("ativo", acompanhamento.GetProperty("estado").GetString());
        Assert.Equal(-23.55, acompanhamento.GetProperty("posicao").GetProperty("lat").GetDouble());
        Assert.False(acompanhamento.TryGetProperty("fichaMedica", out _));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsync($"/api/v1/sos/{id}/{acao}", null)).StatusCode);
        factory.Clock.Agora = factory.Clock.Agora.AddHours(1);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsync($"/api/v1/sos/{id}/{acao}", null)).StatusCode);
        acompanhamento = await client.GetFromJsonAsync<JsonElement>($"/api/v1/acompanhar/{token}");
        Assert.Equal(estado, acompanhamento.GetProperty("estado").GetString());
        var outraAcao = acao == "cancelar" ? "encerrar" : "cancelar";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsync($"/api/v1/sos/{id}/{outraAcao}", null)).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<GarupaDbContext>().Sos.CountAsync());
        factory.Clock.Agora = factory.Clock.Agora.AddHours(11);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/acompanhar/{token}")).StatusCode);
    }

    [Fact]
    public async Task MesmoEventoComOutroConteudoRetornaConflito()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cadastrar(client);
        var evento = Guid.NewGuid().ToString();
        await client.PostAsJsonAsync("/api/v1/sos", Evento(id, evento, factory.Clock.Agora));
        var resposta = await client.PostAsJsonAsync("/api/v1/sos", Evento(id, evento, factory.Clock.Agora.AddMinutes(-1)));
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task SosAutomaticamenteEncerraEExpiraToken()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cadastrar(client);
        var resposta = await client.PostAsJsonAsync("/api/v1/sos", Evento(id, Guid.NewGuid().ToString(), factory.Clock.Agora));
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var token = criado.GetProperty("tokenAcompanhamento").GetString();
        factory.Clock.Agora = factory.Clock.Agora.AddHours(12);
        var acompanhamento = await client.GetFromJsonAsync<JsonElement>($"/api/v1/acompanhar/{token}");
        Assert.Equal("encerrado", acompanhamento.GetProperty("estado").GetString());
        factory.Clock.Agora = factory.Clock.Agora.AddHours(12);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/acompanhar/{token}")).StatusCode);
    }

    [Fact]
    public async Task RecursosInexistentesRetornam404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/acompanhar/inexistente")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsync("/api/v1/sos/999/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsync("/api/v1/sos/999/encerrar", null)).StatusCode);
        var resposta = await client.PostAsJsonAsync("/api/v1/sos", Evento(999, Guid.NewGuid().ToString(), factory.Clock.Agora));
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Theory]
    [InlineData("eventoId", "invalido")]
    [InlineData("tipo", "desconhecido")]
    [InlineData("origem", "desconhecida")]
    [InlineData("ocorridoEm", "2026-11-09T13:00:00Z")]
    [InlineData("ocorridoEm", "2026-11-09T08:00:00-03:00")]
    [InlineData("lat", "91")]
    [InlineData("lon", "181")]
    [InlineData("lat", null)]
    [InlineData("ocorridoEm", null)]
    public async Task SosRejeitaDadosInvalidosSemExporPayload(string campo, string? valor)
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cadastrar(client);
        var dados = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            JsonSerializer.Serialize(Evento(id, Guid.NewGuid().ToString(), factory.Clock.Agora)))!;
        if (valor is null) dados.Remove(campo);
        else dados[campo] = campo is "lat" or "lon" ? int.Parse(valor) : valor;
        var resposta = await client.PostAsJsonAsync("/api/v1/sos", dados);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("-23.55", erro);
        Assert.DoesNotContain("-46.63", erro);
        Assert.Contains("application/problem+json", resposta.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task SaudeVerificaBanco()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var resposta = await client.GetFromJsonAsync<JsonElement>("/api/v1/saude");
        Assert.Equal("conectado", resposta.GetProperty("banco").GetString());
    }
}
