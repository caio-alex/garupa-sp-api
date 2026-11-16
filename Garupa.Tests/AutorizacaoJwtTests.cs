using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Garupa.Tests;

public class AutorizacaoJwtTests
{
    [Fact]
    public async Task CadastroEmiteTokenEProtegeDadosEntreUsuarios()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var primeiro = await CadastrarAsync(client, "11987654321");
        var segundo = await CadastrarAsync(client, "11987654322");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/usuarios/{primeiro.Id}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/usuarios/{primeiro.Id}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenAssinadoPorOutraChave(primeiro.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/usuarios/{primeiro.Id}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenExpirado(primeiro.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/usuarios/{primeiro.Id}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", primeiro.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/usuarios/{primeiro.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/usuarios/{segundo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/v1/usuarios/{segundo.Id}/ficha-medica", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/eventos-vestivel/pendentes?usuarioId={segundo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/distritos")).StatusCode);
    }

    [Fact]
    public async Task SosEVestivelExigemProprietarioEAcompanhamentoAceitaLink()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var primeiro = await CadastrarAsync(client, "11987654321");
        var segundo = await CadastrarAsync(client, "11987654322");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", primeiro.Token);
        using var parear = await client.PostAsJsonAsync($"/api/v1/usuarios/{primeiro.Id}/vestivel", new { dispositivoId = "teste" });
        Assert.Equal(HttpStatusCode.NoContent, parear.StatusCode);
        using var sos = await client.PostAsJsonAsync("/api/v1/sos", new
        {
            eventoId = Guid.NewGuid().ToString(), usuarioId = primeiro.Id, tipo = "queda", origem = "celular",
            ocorridoEm = factory.Clock.Agora, lat = -23.55, lon = -46.63
        });
        Assert.Equal(HttpStatusCode.Created, sos.StatusCode);
        var dados = await sos.Content.ReadFromJsonAsync<JsonElement>();
        var sosId = dados.GetProperty("sosId").GetInt32();
        var acompanhamento = dados.GetProperty("tokenAcompanhamento").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", segundo.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsync($"/api/v1/sos/{sosId}/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/vestivel/teste/alertas/pendentes")).StatusCode);
        using var evento = await client.PostAsJsonAsync("/api/v1/eventos-vestivel", new
        {
            dispositivoId = "teste", tipo = "impacto", ocorridoEm = factory.Clock.Agora
        });
        Assert.Equal(HttpStatusCode.Forbidden, evento.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/acompanhar/{acompanhamento}")).StatusCode);
    }

    private static async Task<(int Id, string Token)> CadastrarAsync(HttpClient client, string telefone)
    {
        using var resposta = await client.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Pessoa fictícia", telefone, senha = "senha-teste-123",
            contato = new { nome = "Contato fictício", telefone = "1134567890" }
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("usuarioId").GetInt32(), json.GetProperty("token").GetString()!);
    }

    private static string TokenExpirado(int usuarioId)
    {
        var chave = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var token = new JwtSecurityToken("garupa-api", "garupa-app",
            [new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string TokenAssinadoPorOutraChave(int usuarioId)
    {
        var chave = Enumerable.Repeat((byte)42, 32).ToArray();
        var token = new JwtSecurityToken("garupa-api", "garupa-app",
            [new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(chave), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
