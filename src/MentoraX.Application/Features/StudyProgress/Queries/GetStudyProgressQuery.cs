using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyProgress.Queries;

public sealed class GetStudyProgressQuery : IQuery<StudyProgressDto?>
{
    public Guid MaterialId { get; set; }

    public GetStudyProgressQuery(Guid materialId)
    {
        MaterialId = materialId;
    }
}

public sealed class GetStudyProgressQueryHandler : IQueryHandler<GetStudyProgressQuery, StudyProgressDto?>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public GetStudyProgressQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<StudyProgressDto?> Handle(GetStudyProgressQuery request, CancellationToken cancellationToken)
    {
        var progress = await _dbContext.StudyProgresses
            .FirstOrDefaultAsync(x =>
                x.LearningMaterialId == request.MaterialId &&
                x.UserId == _currentUser.GetUserId(),
                cancellationToken);

        if (progress is null)
            return null;

        var performanceLevel = GetPerformanceLevel(progress.EasinessFactor);

        return new StudyProgressDto
        {
            MaterialId = request.MaterialId,
            RepetitionCount = progress.RepetitionCount,
            IntervalDays = progress.IntervalDays,
            EasinessFactor = progress.EasinessFactor,
            SuccessStreak = progress.SuccessStreak,
            FailureCount = progress.FailureCount,
            NextReviewAtUtc = progress.NextReviewAtUtc,
            PerformanceLevel = performanceLevel,
            NextReviewReason = GenerateReason(progress)
        };
    }

    private static string GetPerformanceLevel(double ef)
    {
        if (ef >= 2.5) return "Strong";
        if (ef >= 2.0) return "Medium";
        return "Weak";
    }

    private static string GenerateReason(Domain.Entities.StudyProgress progress)
    {
        return $"Your repetition count is {progress.RepetitionCount}, " +
               $"so the system scheduled the next review after {progress.IntervalDays} days.";
    }
}