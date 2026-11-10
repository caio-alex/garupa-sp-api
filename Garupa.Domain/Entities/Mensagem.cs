namespace Garupa.Domain.Entities;

public class Mensagem
{
    public int Id { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public string Contexto { get; private set; } = string.Empty;
    public string? TipoDeUso { get; private set; }
    public string Fonte { get; private set; } = string.Empty;
    public bool Ativa { get; private set; }
    private Mensagem() { }

    public Mensagem(int id, string texto, string contexto, string? tipoDeUso, string fonte)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > 140 || !ContextoValido(contexto) ||
            (tipoDeUso is not null && !PerfilUso.TiposPermitidos.Contains(tipoDeUso)) ||
            !Uri.TryCreate(fonte, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new ArgumentException("Mensagem inválida.");
        Id = id;
        Texto = texto;
        Contexto = contexto;
        TipoDeUso = tipoDeUso;
        Fonte = fonte;
        Ativa = true;
    }

    public static bool ContextoValido(string contexto)
        => contexto is "chuva" or "noite" or "domingo" or "ponto_critico" or "geral";
}
