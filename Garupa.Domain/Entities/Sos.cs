using Garupa.Domain.Exceptions;
using Garupa.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Garupa.Domain.Entities;

public class Sos
{
    public int Id { get; private set; }
    public string EventoId { get; private set; } = string.Empty;
    public int UsuarioId { get; private set; }
    public TipoEmergencia Tipo { get; private set; }
    public string Origem { get; private set; } = string.Empty;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public EstadoSos Estado { get; private set; }
    public string TokenAcompanhamento { get; private set; } = string.Empty;
    public string? TokenHash { get; private set; }
    public DateTime OcorridoEm { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? TerminadoEm { get; private set; }

    private Sos() { }

    public Sos(string eventoId, int usuarioId, TipoEmergencia tipo, string origem,
        double latitude, double longitude, DateTime ocorridoEm, DateTime agora)
    {
        if (!Guid.TryParse(eventoId, out var evento) || evento == Guid.Empty)
            throw new ArgumentException("Evento inválido.");
        if (usuarioId <= 0 || !Enum.IsDefined(tipo) || origem is not ("celular" or "vestivel"))
            throw new ArgumentException("Dados do SOS inválidos.");
        if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90 ||
            !double.IsFinite(longitude) || longitude < -180 || longitude > 180)
            throw new ArgumentException("Posição inválida.");
        if (ocorridoEm.Kind != DateTimeKind.Utc || agora.Kind != DateTimeKind.Utc || ocorridoEm > agora)
            throw new ArgumentException("Horário inválido.");

        EventoId = evento.ToString("D");
        UsuarioId = usuarioId;
        Tipo = tipo;
        Origem = origem;
        Latitude = latitude;
        Longitude = longitude;
        // PostgreSQL armazena timestamps com precisão de microssegundos.
        OcorridoEm = new DateTime(ocorridoEm.Ticks / 10 * 10, DateTimeKind.Utc);
        CriadoEm = agora;
        Estado = EstadoSos.Ativo;
        TokenAcompanhamento = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        TokenHash = CalcularHash(TokenAcompanhamento);
    }

    public static string CalcularHash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public void AtualizarExpiracao(DateTime agora)
    {
        if (Estado == EstadoSos.Ativo && agora >= CriadoEm.AddHours(12))
        {
            Estado = EstadoSos.Encerrado;
            TerminadoEm = CriadoEm.AddHours(12);
        }
    }

    public bool TokenExpirou(DateTime agora)
        => TerminadoEm.HasValue && agora >= TerminadoEm.Value.AddHours(12);

    public void Cancelar(DateTime agora)
    {
        AtualizarExpiracao(agora);
        if (Estado == EstadoSos.Encerrado)
            throw new ConflitoSosException("SOS já encerrado.");
        if (Estado == EstadoSos.Ativo)
        {
            Estado = EstadoSos.Cancelado;
            TerminadoEm = agora;
        }
    }

    public void Encerrar(DateTime agora)
    {
        AtualizarExpiracao(agora);
        if (Estado == EstadoSos.Cancelado)
            throw new ConflitoSosException("SOS já cancelado.");
        if (Estado == EstadoSos.Ativo)
        {
            Estado = EstadoSos.Encerrado;
            TerminadoEm = agora;
        }
    }
}
