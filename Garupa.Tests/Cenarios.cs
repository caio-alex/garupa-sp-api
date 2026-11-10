using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Garupa.Tests;

internal static class Cenarios
{
    public static async Task<int> CadastrarAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Pessoa fictícia", telefone = "11987654321",
            contato = new { nome = "Contato fictício", telefone = "1134567890" }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetInt32();
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
