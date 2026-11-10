
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
        Atualizar(tipoSanguineo, alergias, condicoes, medicamentos);
    }

    public void Atualizar(string tipoSanguineo, string alergias, string condicoes, string medicamentos)
    {
        if (tipoSanguineo is not ("" or "A+" or "A-" or "B+" or "B-" or "AB+" or "AB-" or "O+" or "O-"))
            throw new ArgumentException("Tipo sanguíneo inválido.");
        if (alergias.Length > 2000 || condicoes.Length > 2000 || medicamentos.Length > 2000)
            throw new ArgumentException("Ficha muito extensa.");
        TipoSanguineo = tipoSanguineo;
        Alergias = alergias;
        Condicoes = condicoes;
        Medicamentos = medicamentos;
    }
}
