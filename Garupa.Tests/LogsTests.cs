using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Garupa.Tests;

public class LogsTests
{
    [Fact]
    public async Task LogsUsamRotaECorrelacaoSemTelefoneCoordenadasFichaOuToken()
    {
        using var factory = new ApiFactory();
        using var client = factory.CriarCliente();
        var id = await Cenarios.CadastrarAsync(client);
        await Cenarios.ConsentirAsync(client, id);
        await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/ficha-medica", new { alergias = "Alergia_ficticia_confidencial" });
        var response = await client.PostAsJsonAsync("/api/v1/sos", new
        {
            eventoId = Guid.NewGuid(), usuarioId = id, tipo = "queda", origem = "celular",
            ocorridoEm = factory.Clock.Agora, lat = -23.55, lon = -46.63
        });
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tokenAcompanhamento").GetString()!;
        using var acompanhamento = await client.GetAsync($"/api/v1/acompanhar/{token}");
        Assert.True(acompanhamento.Headers.Contains("X-Correlation-ID"));
        var logs = string.Join('\n', factory.Logs);
        Assert.Contains("acompanhar/{token}", logs);
        Assert.DoesNotContain("11987654321", logs);
        Assert.DoesNotContain("Alergia_ficticia_confidencial", logs);
        Assert.DoesNotContain("-23.55", logs);
        Assert.DoesNotContain("-46.63", logs);
        Assert.DoesNotContain(token, logs);
    }
}
