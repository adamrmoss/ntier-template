using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTierTemplate.Application.Queue;

namespace NTierTemplate.Queue.RabbitMq;

/// <summary>
/// Periodically republishes failed commands that are due for retry.
/// </summary>
public class FailedCommandRetryService(
    IServiceScopeFactory scopeFactory,
    ILogger<FailedCommandRetryService> logger
)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queueApplicationService = scope.ServiceProvider.GetRequiredService<IQueueApplicationService>();
                await queueApplicationService.RetryDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed command retry sweep failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
