using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public interface IConteudoRepository
{
    Task<IReadOnlyList<Mensagem>> MensagensAsync(string? contexto, string[] tipos, CancellationToken ct);
    Task<IReadOnlyList<CelulaRisco>> RiscoPorRaioAsync(double lat, double lon, double raio,
        int faixa, string tipoDia, bool chuva, CancellationToken ct);
    Task<IReadOnlyList<CelulaRisco>> DetalheCelulaAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<CelulaRisco>> RiscoPorDistritosAsync(int[] distritos, int faixa, string tipoDia, bool chuva, CancellationToken ct);
}
