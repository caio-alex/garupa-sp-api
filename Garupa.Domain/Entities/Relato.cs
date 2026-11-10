namespace Garupa.Domain.Entities;

public class Relato
{
    public int Id { get; private set; }
    public string EventoId { get; private set; } = string.Empty;
    public int UsuarioId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    private Relato() { }
    public Relato(string eventoId, int usuarioId, string tipo, double lat, double lon, string descricao)
    {
        Posicao.Validar(lat, lon);
        if (!Guid.TryParse(eventoId, out var evento) || evento == Guid.Empty || usuarioId <= 0 ||
            string.IsNullOrWhiteSpace(tipo) || tipo.Length > 40 || descricao.Length > 280)
            throw new ArgumentException("Relato inválido.");
        EventoId = evento.ToString("D");
        UsuarioId = usuarioId;
        Tipo = tipo.Trim();
        Latitude = lat;
        Longitude = lon;
        Descricao = descricao.Trim();
    }
}
