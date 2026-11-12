using System.Text.Json;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace Garupa.Tests;

public class SwaggerTests
{
    [Fact]
    public async Task SwaggerUiEJsonFicamDisponiveisEmProducao()
    {
        using var factory = new ApiFactory();
        using var production = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                // A verificação das rotas de documentação dispensa a manutenção periódica do banco.
                var manutencao = services.Single(d => d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(Garupa.Api.Services.ManutencaoBackgroundService));
                services.Remove(manutencao);
            });
        });
        using var client = production.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        Assert.True(production.Services.GetRequiredService<IHostEnvironment>().IsProduction());

        using var ui = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        Assert.Contains("Swagger UI", await ui.Content.ReadAsStringAsync());

        using var json = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, json.StatusCode);
        using var document = JsonDocument.Parse(await json.Content.ReadAsStringAsync());
        Assert.Equal("Garupa SP API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/v1/sos", out _));
    }

    [Fact]
    public void SwaggerDocumentaStatusSchemasExemplosEFaixaOpcional()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        document.SerializeAsV3(new OpenApiJsonWriter(buffer));
        using var json = JsonDocument.Parse(buffer.ToString());
        var paths = json.RootElement.GetProperty("paths");
        var cadastro = paths.GetProperty("/api/v1/usuarios").GetProperty("post");
        Assert.True(cadastro.GetProperty("responses").TryGetProperty("201", out _));
        Assert.False(cadastro.GetProperty("responses").TryGetProperty("200", out _));
        var exemplo = cadastro.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("example");
        Assert.True(exemplo.TryGetProperty("contato", out _));
        var sos = paths.GetProperty("/api/v1/sos").GetProperty("post").GetProperty("responses");
        Assert.True(sos.GetProperty("409").GetProperty("content").TryGetProperty("application/problem+json", out _));
        var faixa = paths.GetProperty("/api/v1/plano-do-turno").GetProperty("get").GetProperty("parameters")
            .EnumerateArray().Single(p => p.GetProperty("name").GetString() == "faixaHoraria");
        Assert.False(faixa.TryGetProperty("required", out var required) && required.GetBoolean());
        Assert.Contains("habitual", faixa.GetProperty("description").GetString());
    }
}
