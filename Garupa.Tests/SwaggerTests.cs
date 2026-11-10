using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace Garupa.Tests;

public class SwaggerTests
{
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
