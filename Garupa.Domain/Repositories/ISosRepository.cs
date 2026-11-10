using Garupa.Domain.Entities;

namespace Garupa.Domain.Repositories;

public interface ISosRepository
{
    Task<Sos?> ObterPorEventoAsync(string eventoId, CancellationToken cancellationToken);
    Task<Sos?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
    Task<Sos?> ObterPorTokenAsync(string token, CancellationToken cancellationToken);
    Task<Sos> AdicionarAsync(Sos sos, CancellationToken cancellationToken);
    Task SalvarAsync(CancellationToken cancellationToken);
}
