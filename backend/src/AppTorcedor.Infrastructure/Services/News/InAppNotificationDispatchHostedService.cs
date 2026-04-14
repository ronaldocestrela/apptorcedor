using AppTorcedor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.News;

public sealed class InAppNotificationDispatchHostedService(
    IServiceProvider services,
    IHostEnvironment environment,
    ILogger<InAppNotificationDispatchHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
        {
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // host shutdown
            }

            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var dispatch = scope.ServiceProvider.GetRequiredService<IInAppNotificationDispatchService>();
                var n = await dispatch.ProcessDueAsync(stoppingToken).ConfigureAwait(false);
                if (n > 0)
                    logger.LogInformation("In-app notifications dispatched: {Count}", n);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "In-app notification dispatch failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
