using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record CancelStudyPlanCommand(Guid StudyPlanId) : ICommand<int>;

public sealed class CancelStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CancelStudyPlanCommand, int>
{
    public async Task<int> Handle(
        CancelStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var plan = await dbContext.StudyPlans
            .Include(x => x.Items)
                .ThenInclude(x => x.StudySessions)
            .FirstOrDefaultAsync(
                x => x.Id == command.StudyPlanId && x.UserId == userId,
                cancellationToken);

        if (plan is null)
            throw new AppNotFoundException(
                "Study plan was not found.",
                "study_plan_not_found");

        if (plan.Status == PlanStatus.Completed)
            throw new AppConflictException(
                "Completed plan cannot be cancelled.",
                "completed_plan_cannot_be_cancelled");

        plan.Status = PlanStatus.Cancelled;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var item in plan.Items)
        {
            if (item.Status != StudyPlanItemStatus.Completed)
            {
                item.Cancel();
            }

            foreach (var session in item.StudySessions)
            {
                if (!session.IsCompleted)
                {
                    session.UpdatedAtUtc = DateTime.UtcNow;
                    // Eğer StudySession içinde Cancel/Status alanı yoksa şimdilik sadece UpdatedAtUtc yeterli.
                    // Sonra StudySessionStatus ekleyip session.Cancel() yapabiliriz.
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return 1;
    }
}