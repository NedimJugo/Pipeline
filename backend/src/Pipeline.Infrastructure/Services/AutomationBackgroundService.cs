using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pipeline.Application.Features.Automation.Services;

namespace Pipeline.Infrastructure.Services;

public class AutomationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomationBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(1);

    public AutomationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AutomationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationBackgroundService started.");

        // Wait a few seconds on startup before first check
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var ruleEngine = scope.ServiceProvider.GetRequiredService<IAutomationRuleEngine>();
                var createdCount = await ruleEngine.EvaluateAllActiveUsersAsync(stoppingToken);
                if (createdCount > 0)
                {
                    _logger.LogInformation("Automation rule engine generated {Count} next-action tasks.", createdCount);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred executing automation rule engine.");
            }

            try
            {
                await Task.Delay(_period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("AutomationBackgroundService stopped.");
    }
}
