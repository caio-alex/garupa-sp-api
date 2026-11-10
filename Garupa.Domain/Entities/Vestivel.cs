namespace Garupa.Domain.Entities;

public class Vestivel
{
    public int UsuarioId { get; private set; }
    public string DispositivoId { get; private set; } = string.Empty;
    private Vestivel() { }
    public Vestivel(int usuarioId, string dispositivoId)
    {
        ValidarDispositivo(dispositivoId);
        UsuarioId = usuarioId;
        DispositivoId = dispositivoId;
    }
    public static void ValidarDispositivo(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Any(char.IsControl))
            throw new ArgumentException("Dispositivo inválido.");
    }
}

public class EventoVestivel
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string DispositivoId { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public DateTime OcorridoEm { get; private set; }
    public DateTime? ConsumidoEm { get; private set; }
    private EventoVestivel() { }
    public EventoVestivel(int usuarioId, string dispositivoId, string tipo, DateTime ocorridoEm, DateTime agora)
    {
        Vestivel.ValidarDispositivo(dispositivoId);
        if (tipo is not ("impacto" or "sos" or "cancelar")) throw new ArgumentException("Evento inválido.");
        ValidarHorario(ocorridoEm, agora);
        UsuarioId = usuarioId;
        DispositivoId = dispositivoId;
        Tipo = tipo;
        OcorridoEm = ocorridoEm;
    }
    public static void ValidarHorario(DateTime ocorridoEm, DateTime agora)
    {
        if (ocorridoEm.Kind != DateTimeKind.Utc || ocorridoEm > agora)
            throw new ArgumentException("Horário inválido.");
    }
}

public class AlertaVestivel
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string DispositivoId { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public DateTime OcorridoEm { get; private set; }
    public DateTime? ConsumidoEm { get; private set; }
    private AlertaVestivel() { }
    public AlertaVestivel(int usuarioId, string dispositivoId, string tipo, DateTime ocorridoEm, DateTime agora)
    {
        Vestivel.ValidarDispositivo(dispositivoId);
        if (tipo is not ("turno_ativo" or "ponto_critico" or "mensagem" or "contagem_queda" or
            "sos_enviado" or "cancelado" or "turno_encerrado")) throw new ArgumentException("Alerta inválido.");
        EventoVestivel.ValidarHorario(ocorridoEm, agora);
        UsuarioId = usuarioId;
        DispositivoId = dispositivoId;
        Tipo = tipo;
        OcorridoEm = ocorridoEm;
    }
}
