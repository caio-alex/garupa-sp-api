using System.Text.Json;

namespace Garupa.Domain.Entities;

public record HorarioHabitual(int FaixaHoraria, string TipoDia);

public class PerfilUso
{
    public static readonly string[] TiposPermitidos = ["entregador_app", "trabalho_moto", "ida_volta", "lazer"];
    public int UsuarioId { get; private set; }
    public string TiposDeUsoJson { get; private set; } = "[]";
    public string HorariosHabituaisJson { get; private set; } = "[]";
    public List<DistritoFrequente> Distritos { get; private set; } = [];

    private PerfilUso() { }
    public PerfilUso(int usuarioId) => UsuarioId = usuarioId;

    public string[] ObterTipos() => JsonSerializer.Deserialize<string[]>(TiposDeUsoJson)!;
    public HorarioHabitual[] ObterHorarios() => JsonSerializer.Deserialize<HorarioHabitual[]>(HorariosHabituaisJson)!;

    public void Atualizar(string[] tipos, HorarioHabitual[] horarios, int[] distritos)
    {
        if (tipos.Length is < 1 or > 4 || tipos.Distinct().Count() != tipos.Length ||
            tipos.Any(t => !TiposPermitidos.Contains(t)) || horarios.Length > 18 ||
            horarios.Any(h => h is null || h.FaixaHoraria is < 0 or > 5 || !ContextoRisco.TipoDiaValido(h.TipoDia)) ||
            horarios.Distinct().Count() != horarios.Length || distritos.Length > 5 ||
            distritos.Any(id => id <= 0) || distritos.Distinct().Count() != distritos.Length)
            throw new ArgumentException("Perfil inválido.");
        TiposDeUsoJson = JsonSerializer.Serialize(tipos);
        HorariosHabituaisJson = JsonSerializer.Serialize(horarios);
        Distritos.RemoveAll(d => !distritos.Contains(d.DistritoId));
        foreach (var id in distritos.Except(Distritos.Select(d => d.DistritoId)))
            Distritos.Add(new DistritoFrequente(UsuarioId, id));
    }
}

public class DistritoFrequente
{
    public int UsuarioId { get; private set; }
    public int DistritoId { get; private set; }
    private DistritoFrequente() { }
    public DistritoFrequente(int usuarioId, int distritoId) => (UsuarioId, DistritoId) = (usuarioId, distritoId);
}

public class Distrito
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    private Distrito() { }
    public Distrito(int id, string nome) => (Id, Nome) = (id, nome);
}

public static class ContextoRisco
{
    public static bool TipoDiaValido(string tipo) => tipo is "util" or "sabado" or "domingo";
    public static void Validar(int faixaHoraria, string tipoDia)
    {
        if (faixaHoraria is < 0 or > 5 || !TipoDiaValido(tipoDia))
            throw new ArgumentException("Contexto inválido.");
    }
}
