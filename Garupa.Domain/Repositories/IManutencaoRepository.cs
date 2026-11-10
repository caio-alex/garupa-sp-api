namespace Garupa.Domain.Repositories;

public interface IManutencaoRepository
{
    Task ExecutarAsync(DateTime agora, CancellationToken ct);
}
