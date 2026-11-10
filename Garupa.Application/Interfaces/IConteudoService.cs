using Garupa.Domain.Entities;

namespace Garupa.Application.Interfaces;

public record PontoCritico(string CelulaId, double CentroLat, double CentroLon, double Score,
    int Percentil, string ViaReferencia, int DistritoId, string VersaoModelo, string Motivo);
public record MensagemResposta(int Id, string Texto, string Fonte);
public record PlanoTurno(IReadOnlyList<PontoCritico> PontosCriticos, IReadOnlyList<MensagemResposta> Mensagens,
    int FaixaHoraria, string OrigemFaixa);

public interface IConteudoService
{
    Task<IReadOnlyList<MensagemResposta>> MensagensAsync(string? contexto, string? tipoDeUso, CancellationToken ct);
    Task<IReadOnlyList<PontoCritico>> RiscoAsync(double lat, double lon, double raio, int faixa, string tipoDia, bool chuva, CancellationToken ct);
    Task<IReadOnlyList<CelulaRisco>> DetalheAsync(string id, CancellationToken ct);
    Task<PlanoTurno?> PlanoAsync(int usuarioId, int? faixa, string tipoDia, bool chuva, CancellationToken ct);
}
