using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Garupa.Api.Security;

namespace Garupa.Api.Services;

public sealed class ExplicadorGemini(HttpClient http, IConfiguration config)
{
    public async Task<(string Mensagem, string Origem)> Explicar(EventoCadastro evento, CancellationToken ct)
    {
        var reserva = ("Os dados pessoais desta operação foram ocultados no registro técnico.", "Mensagem local");
        var chave = config["Gemini:ApiKey"];
        var modelo = config["Gemini:Modelo"];
        if (string.IsNullOrWhiteSpace(chave) || modelo is null ||
            !Regex.IsMatch(modelo, @"\A[a-zA-Z0-9._-]{1,100}\z")) return reserva;

        var texto = "Explique em português do Brasil, em até duas frases simples, ao titular dos dados. " +
            "A API impediu a gravação dos valores pessoais no log. Não há evidência de invasão. " +
            "Não invente fatos. Operação: " + evento.Fonte + ". Campos protegidos: " + string.Join(", ", evento.Campos);
        using var pedido = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent");
        pedido.Headers.Add("x-goog-api-key", chave);
        pedido.Content = JsonContent.Create(new { contents = new[] { new { parts = new[] { new { text = texto } } } } });
        try
        {
            using var resposta = await http.SendAsync(pedido, ct);
            if (!resposta.IsSuccessStatusCode) return reserva;
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));
            var candidato = json.RootElement.GetProperty("candidates")[0];
            if (candidato.GetProperty("finishReason").GetString() != "STOP") return reserva;
            var partes = candidato.GetProperty("content").GetProperty("parts").EnumerateArray();
            var mensagem = string.Join(" ", partes.Where(p => !p.TryGetProperty("thought", out var t) || t.ValueKind != JsonValueKind.True)
                .Where(p => p.TryGetProperty("text", out _)).Select(p => p.GetProperty("text").GetString())).Trim();
            return mensagem.Length is > 0 and <= 480 ? (mensagem, "Gemini") : reserva;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return reserva; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
            or InvalidOperationException or IndexOutOfRangeException) { return reserva; }
    }
}
