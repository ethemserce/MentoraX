using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record CompleteStudyPlanCommand(Guid StudyPlanId) : ICommand<int>;

public sealed class CompleteStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<CompleteStudyPlanCommand, int>
{
    public async Task<int> Handle(
        CompleteStudyPlanCommand command,
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
        {
            throw new AppNotFoundException(
                "Study plan was not found.",
                "study_plan_not_found");
        }

        if (plan.Status == PlanStatus.Cancelled)
        {
            throw new AppConflictException(
                "Cancelled plan cannot be completed.",
                "cancelled_plan_cannot_be_completed");
        }

        if (plan.Status == PlanStatus.Completed)
        {
            return 1;
        }

        var now = DateTime.UtcNow;

        plan.Status = PlanStatus.Completed;
        plan.UpdatedAtUtc = now;

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
                    session.UpdatedAtUtc = now;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return 1;
    }
}