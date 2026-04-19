using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Mobile.Queries;

public sealed record GetMobileProgressSummaryQuery(Guid UserId) : IQuery<MobileProgressSummaryDto>;

public sealed class GetMobileProgressSummaryQueryHandler(IApplicationDbContext _dbContext)
    : IQueryHandler<GetMobileProgressSummaryQuery, MobileProgressSummaryDto>
{
    public async Task<MobileProgressSummaryDto> Handle(
        GetMobileProgressSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var totalMaterials = await _dbContext.LearningMaterials
            .AsNoTracking()
            .CountAsync(x => x.UserId == query.UserId, cancellationToken);

        var activePlans = await _dbContext.StudyPlans
            .AsNoTracking()
            .CountAsync(x =>
                x.UserId == query.UserId &&
                x.Status.IsActive(),
                cancellationToken);

        var progresses = await _dbContext.StudyProgresses
            .AsNoTracking()
            .Where(x => x.UserId == query.UserId)
            .Select(x => x.EasinessFactor)
            .ToListAsync(cancellationToken);

        var strongCount = progresses.Count(x => x >= 2.5);
        var mediumCount = progresses.Count(x => x >= 2.0 && x < 2.5);
        var weakCount = progresses.Count(x => x < 2.0);

        var todayCompletedSessions = await _dbContext.StudySessions
            .AsNoTracking()
            .CountAsync(x =>
                x.UserId == query.UserId &&
                x.IsCompleted &&
                x.CompletedAtUtc.HasValue &&
                DateOnly.FromDateTime(x.CompletedAtUtc.Value) == today,
                cancellationToken);

        var completedDays = await _dbContext.StudySessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == query.UserId &&
                x.IsCompleted &&
                x.CompletedAtUtc.HasValue)
            .Select(x => DateOnly.FromDateTime(x.CompletedAtUtc!.Value))
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);

        var currentStreakDays = CalculateStreak(completedDays, today);

        return new MobileProgressSummaryDto(
            totalMaterials,
            activePlans,
            strongCount,
            mediumCount,
            weakCount,
            todayCompletedSessions,
            currentStreakDays);
    }

    private static int CalculateStreak(IReadOnlyCollection<DateOnly> completedDays, DateOnly today)
    {
        if (completedDays.Count == 0)
            return 0;

        var set = completedDays.ToHashSet();

        var streak = 0;
        var cursor = today;

        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}