namespace Garupa.Api.Security;

public sealed record EventoCadastro(DateTimeOffset Data, string Correlacao, string[] Campos,
    OrigemPii Fonte = OrigemPii.Cadastro);

public sealed class AuditoriaCadastro(ILogger<AuditoriaCadastro> logger)
{
    private readonly Queue<(int Dono, EventoCadastro Evento)> historico = new();
    private readonly object trava = new();
    private readonly DetectorPii detector = new();
    private readonly LoggerProtegido logProtegido = new(logger);

    public DadosRastreados Marcar(object entrada) => detector.Inspecionar(entrada);

    public void Registrar(int usuarioId, object entrada, string correlacao)
        => Registrar(usuarioId, Marcar(entrada), correlacao, OrigemPii.Cadastro);

    public void Registrar(int usuarioId, DadosRastreados dados, string correlacao, OrigemPii fonte)
    {
        if (!logProtegido.Registrar(dados, fonte, correlacao)) return;
        var evento = new EventoCadastro(DateTimeOffset.UtcNow, correlacao,
            dados.Campos.Where(c => c.Sensivel).Select(c => c.Caminho).ToArray(), fonte);
        lock (trava)
        {
            if (historico.Count >= 500) historico.Dequeue();
            historico.Enqueue((usuarioId, evento));
        }
    }

    public EventoCadastro? Ultimo(int usuarioId)
    {
        lock (trava) return historico.LastOrDefault(x => x.Dono == usuarioId).Evento;
    }
}
