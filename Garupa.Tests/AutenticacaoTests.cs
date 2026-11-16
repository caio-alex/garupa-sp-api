using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Garupa.Tests;

public class AutenticacaoTests
{
    [Fact]
    public async Task LoginExigeSenhaCorretaEEmiteJwt()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var cadastro = await client.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Pessoa fictícia", telefone = "11987654321", senha = "senha-teste-123",
            contato = new { nome = "Contato fictício", telefone = "1134567890" }
        });
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);

        var invalido = await client.PostAsJsonAsync("/api/v1/autenticacao/login",
            new { telefone = "11987654321", senha = "senha-errada" });
        Assert.Equal(HttpStatusCode.Unauthorized, invalido.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/autenticacao/login",
            new { telefone = "+55 (11) 98765-4321", senha = "senha-teste-123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var resposta = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = resposta.RootElement.GetProperty("token").GetString()!;
        var chave = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var identidade = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(chave),
            ValidIssuer = "garupa-api",
            ValidAudience = "garupa-app",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);
        Assert.NotNull(identidade.Identity);
        Assert.True(identidade.Identity.IsAuthenticated);
    }
}
