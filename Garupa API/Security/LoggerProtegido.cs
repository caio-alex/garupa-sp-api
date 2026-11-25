using System.Text.Json;

namespace Garupa.Api.Security;

public sealed class LoggerProtegido(ILogger logger)
{
    public bool Registrar(DadosRastreados dados, OrigemPii origem, string correlacao)
    {
        var protegidos = dados.Campos.Where(c => c.Sensivel).ToArray();
        // Fronteira do sink: nenhuma interpolacao recebe o valor bruto de um dado marcado.
        var saida = dados.Campos.Select(c => new { campo = c.Caminho, valor = c.ParaLog() });
        logger.LogInformation("PII_SINK_WRITE {Origem} -> Log | Dados: {Dados} | {Correlacao}",
            origem, JsonSerializer.Serialize(saida), correlacao);
        if (protegidos.Length == 0) return false;

        logger.LogWarning("PII_LOG_BLOCKED {Origem} -> Log | Campos omitidos: {Campos} | {Correlacao}",
            origem, string.Join(", ", protegidos.Select(c => c.Caminho)), correlacao);
        return true;
    }
}
