using HotelBooking.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Services;

public class HoldExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryService> _logger;

    public HoldExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var db =
                    scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                var expiredHolds = await db.RoomHolds
                    .Where(h =>
                        h.HeldUntil <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expiredHolds.Any())
                {
                    db.RoomHolds.RemoveRange(expiredHolds);

                    await db.SaveChangesAsync(
                        stoppingToken);

                    _logger.LogInformation(
                        "Removed {Count} expired room holds.",
                        expiredHolds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while removing expired room holds.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken);
        }
    }
}