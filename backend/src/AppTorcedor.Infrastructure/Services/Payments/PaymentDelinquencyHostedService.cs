using AppTorcedor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class PaymentDelinquencyHostedService(
    IServiceProvider services,
    IHostEnvironment environment,
    ILogger<PaymentDelinquencyHostedService> logger) : BackgroundService
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
                var sweep = scope.ServiceProvider.GetRequiredService<IPaymentDelinquencySweep>();
                var result = await sweep.RunAsync(stoppingToken).ConfigureAwait(false);
                if (result.PaymentsMarkedOverdue > 0 || result.MembershipsMarkedDelinquent > 0)
                {
                    logger.LogInformation(
                        "Delinquency sweep: paymentsMarkedOverdue={Overdue}, membershipsMarkedDelinquent={Members}",
                        result.PaymentsMarkedOverdue,
                        result.MembershipsMarkedDelinquent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delinquency sweep failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
