using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Garupa.Api.DTOs.Usuarios;
using Garupa.Api.Security;
using Garupa.Api.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Garupa.Tests;
public class AuditoriaCadastroTests {
[Xunit.Fact]
public async Task OmitirValoresEmCadaSaidaEManterHistoricoPorTitular() {
var log = new CapturaLog();
var auditoria = new AuditoriaCadastro(log);
var cadastro = new CriarUsuarioRequest("Sentinela A", "11900000001", new("Sentinela B", "11900000002"), "SegredoSintetico");
auditoria.Registrar(1, cadastro, "teste");
var evento = auditoria.Ultimo(1)!;
if (evento.Campos.Length != 5 || auditoria.Ultimo(2) != null) throw new Exception("Escopo da auditoria");
var transporte = new Transporte();
var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
    { ["Gemini:Modelo"]="modelo", ["Gemini:ApiKey"]="teste" }).Build();
var gemini = new ExplicadorGemini(new HttpClient(transporte), cfg);
if ((await gemini.Explicar(evento, default)).Origem != "Gemini") throw new Exception("Tradução");
var saidas = log.Saida + transporte.Corpo + JsonSerializer.Serialize(evento);
foreach (var valor in new[]{cadastro.Nome,cadastro.Telefone,cadastro.Contato.Nome,cadastro.Contato.Telefone,cadastro.Senha})
    if (saidas.Contains(valor)) throw new Exception("Valor pessoal na saída");
transporte.Status = HttpStatusCode.TooManyRequests;
if ((await gemini.Explicar(evento, default)).Origem != "Mensagem local") throw new Exception("Contingência");
Console.WriteLine("OK: cinco campos, isolamento por titular, log, auditoria, prompt, tradução simulada e HTTP 429.");

}
sealed class Transporte : HttpMessageHandler {
    public string Corpo = "";
    public HttpStatusCode Status = HttpStatusCode.OK;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) {
        Corpo = await req.Content!.ReadAsStringAsync(ct);
        return new(Status){Content=new StringContent("""{"candidates":[{"finishReason":"STOP","content":{"parts":[{"text":"Dados protegidos."}]}}]}""",Encoding.UTF8,"application/json")};
    }
}
sealed class CapturaLog : ILogger<AuditoriaCadastro> {
    public string Saida = "";
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level,EventId id,TState state,Exception? ex,Func<TState,Exception?,string> format) => Saida += format(state,ex);
}

}
