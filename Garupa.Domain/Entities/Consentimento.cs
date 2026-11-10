namespace Garupa.Domain.Entities;

public class Consentimento
{
    public int UsuarioId { get; private set; }
    public bool LocalizacaoEmUso { get; private set; }
    public bool LocalizacaoEmSegundoPlano { get; private set; }
    public bool FichaMedica { get; private set; }
    public bool HistoricoDeLocais { get; private set; }
    public bool PerfilDeUso { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Consentimento() { }
    public Consentimento(int usuarioId) => UsuarioId = usuarioId;

    public void Atualizar(bool localizacaoEmUso, bool localizacaoEmSegundoPlano,
        bool fichaMedica, bool historicoDeLocais, bool perfilDeUso, DateTime agora)
    {
        if (localizacaoEmSegundoPlano && !localizacaoEmUso)
            throw new ArgumentException("Localização em segundo plano exige localização em uso.");
        LocalizacaoEmUso = localizacaoEmUso;
        LocalizacaoEmSegundoPlano = localizacaoEmSegundoPlano;
        FichaMedica = fichaMedica;
        HistoricoDeLocais = historicoDeLocais;
        PerfilDeUso = perfilDeUso;
        AtualizadoEm = agora;
    }
}
