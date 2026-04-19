using MentoraX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Worker;

public sealed class Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MentoraX Worker started at {Time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MentoraXDbContext>();
            var now = DateTime.UtcNow;
            var dueSoon = await dbContext.StudySessions.AsNoTracking()
                .Where(x => x.StudyPlan.Status == MentoraX.Domain.Enums.PlanStatus.Active && x.ScheduledAtUtc <= now.AddHours(12))
                .OrderBy(x => x.ScheduledAtUtc)
                .Take(50)
                .ToListAsync(stoppingToken);
            logger.LogInformation("Found {Count} planned study sessions due within 12 hours.", dueSoon.Count);
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
