using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarRental.Infrastructure.Email.Delivery;

public sealed class EmailDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailDeliveryOptions> options,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    private readonly EmailDeliveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.BackgroundDelivery)
        {
            logger.LogInformation("Background email delivery is off. Queued mail will not be sent by this host.");

            return;
        }

        using var ticks = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollSeconds));

        try
        {
            do
            {
                await DispatchAsync(stoppingToken);
            }
            while (await ticks.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task DispatchAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<EmailDispatcher>().DispatchDueAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The email delivery pass failed. Retrying on the next tick.");
        }
    }
}
