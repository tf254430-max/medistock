using MediStock.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediStock.Application.Services;

public class NotificationRefreshService : BackgroundService
{
    // Refresh on startup, then every 6 hours per FR-NTF-04.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(6);

    private readonly INotificationCache _cache;
    private readonly ILogger<NotificationRefreshService> _logger;

    public NotificationRefreshService(INotificationCache cache, ILogger<NotificationRefreshService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _cache.RefreshAsync(stoppingToken);
                _logger.LogInformation("Notification cache refreshed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Notification cache refresh failed.");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (TaskCanceledException) { /* shutdown */ }
        }
    }
}
