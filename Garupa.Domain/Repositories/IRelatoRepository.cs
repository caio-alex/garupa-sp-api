using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public interface IRelatoRepository
{
    Task<Relato?> ObterPorEventoAsync(string eventoId, CancellationToken ct);
    Task<Relato> AdicionarAsync(Relato relato, CancellationToken ct);
}
