using Garupa.Domain.Repositories;

namespace Garupa.Api.Services;

public class ManutencaoBackgroundService(IServiceScopeFactory scopes, TimeProvider clock,
    IHostEnvironment ambiente, ILogger<ManutencaoBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ambiente.IsEnvironment("Testing")) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IManutencaoRepository>()
                    .ExecutarAsync(clock.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception)
            {
                logger.LogError("Manutenção do banco pendente. Será tentada novamente no próximo ciclo.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
