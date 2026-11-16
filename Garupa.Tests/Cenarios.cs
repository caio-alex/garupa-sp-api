using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using System.Net.Http.Headers;

namespace Garupa.Tests;

internal static class Cenarios
{
    private static int contador;
    public static async Task<int> CadastrarAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Pessoa fictícia", telefone = $"119{Interlocked.Increment(ref contador):D8}", senha = "senha-teste-123",
            contato = new { nome = "Contato fictício", telefone = "1134567890" }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dados = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dados.GetProperty("token").GetString());
        return dados.GetProperty("usuarioId").GetInt32();
    }

    public static async Task ConsentirAsync(HttpClient client, int id, bool ficha = true, bool perfil = true)
    {
        using var response = await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/consentimentos", new
        {
            localizacaoEmUso = true, localizacaoEmSegundoPlano = true,
            fichaMedica = ficha, historicoDeLocais = false, perfilDeUso = perfil
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<HttpResponseMessage> PerfilAsync(HttpClient client, int id, string tipo, int[] distritos)
        => await client.PutAsJsonAsync($"/api/v1/usuarios/{id}/perfil", new
        {
            tiposDeUso = new[] { tipo },
            horariosHabituais = new[] { new { faixaHoraria = 2, tipoDia = "util" } },
            distritosFrequentes = distritos
        });
}
