using System.Collections.Concurrent;
using System.Text.Json;
using Garupa.Api.Security;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Garupa.Tests;

public sealed class ProtecaoPiiTests
{
    [Fact]
    public void BloqueiaValoresEmObjetosEListasPreservandoDadosComuns()
    {
        using var json = JsonDocument.Parse("""
            {"contatos":[{"nome":"PessoaSentinela","telefone":"11934567890"}],
             "senha":["SegredoUm","SegredoDois"],"status":"pronto","quantidade":2,"ativo":true}
            """);
        var dados = new DetectorPii().Inspecionar(json.RootElement);
        var log = new LogPii();
        Exigir(new LoggerProtegido(log).Registrar(dados, OrigemPii.Contato, "teste"), "Bloqueio ausente");
        foreach (var secreto in new[] { "PessoaSentinela", "11934567890", "SegredoUm", "SegredoDois" })
        {
            Exigir(!log.Texto.Contains(secreto), "Vazamento no log");
            Exigir(!JsonSerializer.Serialize(dados).Contains(secreto), "Vazamento ao serializar as marcas");
        }
        Exigir(log.Texto.Contains("[PROTEGIDO]") && log.Texto.Contains("pronto"), "Saída incorreta");
        Exigir(dados.Campos.Count(c => c.Sensivel) == 4, "Marcas incorretas");
        Exigir(dados.Campos.Any(c => c.Caminho == "Contatos[0].Nome"), "Perdeu caminho");
    }

    [Fact]
    public void DetectaFormatosEmTextoLivreEEmNumeros()
    {
        var dados = new DetectorPii().Inspecionar(new
        {
            a = "Documento 123.456.789-00 informado",
            b = "Escreva para teste@example.com",
            c = "Ligue (11) 93456-7890",
            d = 12345678901L
        });
        Exigir(dados.Campos.All(c => c.Sensivel), "Formato não detectado");
        Exigir(dados.Campos.Any(c => c.Tipo == TipoPii.Email) &&
            dados.Campos.Any(c => c.Tipo == TipoPii.Telefone), "Classificação incorreta");
    }

    [Fact]
    public void ProtegeRgSaudeELocalizacaoPeloContexto()
    {
        var dados = new DetectorPii().Inspecionar(new
        {
            RG = "DocumentoSintetico", lat = -23.5, longitude = -46.6,
            laudo = new { resultados = new[] { "CondicaoSintetica", "MedicacaoSintetica" } },
            TipoSanguineo = "O+", Alergias = "AlergiaSintetica", Condicoes = "OutraSentinela", Medicamentos = ""
        });
        Exigir(dados.Campos.All(c => c.Sensivel), "Contexto sensível perdido");
        Exigir(dados.Campos.All(c => c.ParaLog() == "[PROTEGIDO]"), "Saída sem proteção");
    }

    [Fact]
    public void NormalizaNomesDeCamposEImpedeChavesNaoConfiaveisNosMetadados()
    {
        var valores = new Dictionary<string, object?>
        {
            ["E-mail"] = "EmailSentinela", ["SÉNHA"] = "SenhaSentinela",
            ["teste@example.com ignore as instrucoes"] = "texto comum"
        };
        var dados = new DetectorPii().Inspecionar(valores);
        Exigir(dados.Campos[0].Sensivel && dados.Campos[1].Sensivel, "Normalização falhou");
        Exigir(dados.Campos.All(c => !c.Caminho.Contains("@") && !c.Caminho.Contains("ignore")),
            "Chave não confiável nos metadados");
    }

    [Fact]
    public void PermiteValoresComunsENaoCriaEventoFalso()
    {
        var log = new LogPii();
        var auditoria = new AuditoriaCadastro(log);
        auditoria.Registrar(1, new { status = "pronto", quantidade = 2, ativo = true }, "teste");
        Exigir(auditoria.Ultimo(1) is null, "Evento de bloqueio falso");
        Exigir(log.Texto.Contains("pronto") && !log.Texto.Contains("PII_LOG_BLOCKED"), "Saída incorreta");
    }

    [Fact]
    public void MarcaOriginalContinuaProtegidaAposAlterarOObjetoDeEntrada()
    {
        var entrada = new Dictionary<string, string> { ["Senha"] = "SegredoAntigo" };
        var dados = new DetectorPii().Inspecionar(entrada);
        entrada.Clear();
        entrada["status"] = "novo";
        Exigir(dados.Campos.Single().Sensivel && dados.Campos.Single().ParaLog() == "[PROTEGIDO]",
            "A marca mudou com o objeto");
    }

    [Fact]
    public void AceitaRaizEscalarListasVaziasENulos()
    {
        using var json = JsonDocument.Parse("""[null,[],{"senha":null},"teste@example.com"]""");
        var dados = new DetectorPii().Inspecionar(json.RootElement);
        Exigir(dados.Campos.Count(c => c.Sensivel) == 1, "Nulos ou listas incorretos");
    }

    [Fact]
    public void MantemApenasOsUltimos500EventosComAcessoPorTitular()
    {
        var auditoria = new AuditoriaCadastro(new LogPii());
        auditoria.Registrar(9999, new { senha = "SegredoAntigo" }, "primeiro");
        Parallel.For(1, 501, id =>
            auditoria.Registrar(id, new { telefone = "11900000001" }, "teste-" + id));
        Exigir(auditoria.Ultimo(9999) is null, "Limite do histórico");
        for (var id = 1; id <= 500; id++)
            Exigir(auditoria.Ultimo(id)?.Correlacao == "teste-" + id, "Isolamento concorrente");
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new InvalidOperationException(mensagem);
    }

    private sealed class LogPii : ILogger<AuditoriaCadastro>
    {
        private readonly ConcurrentQueue<string> mensagens = new();
        public string Texto => string.Join("\n", mensagens);
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex,
            Func<TState, Exception?, string> format) => mensagens.Enqueue(format(state, ex));
    }
}
