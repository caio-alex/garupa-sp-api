
namespace Garupa.Domain.Entities;

public class FichaMedica
{
    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public string TipoSanguineo { get; private set; } = string.Empty;

    public string Alergias { get; private set; } = string.Empty;

    public string Condicoes { get; private set; } = string.Empty;

    public string Medicamentos { get; private set; } = string.Empty;

    // Construtor utilizado pelo Entity Framework
    private FichaMedica() { }

    public FichaMedica(
        string tipoSanguineo,
        string alergias,
        string condicoes,
        string medicamentos)
    {
        TipoSanguineo = tipoSanguineo;
        Alergias = alergias;
        Condicoes = condicoes;
        Medicamentos = medicamentos;
    }
}
