using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record ResumeStudyPlanCommand(Guid StudyPlanId) : ICommand<int>;

public sealed class ResumeStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<ResumeStudyPlanCommand, int>
{
    public async Task<int> Handle(
        ResumeStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();

        var plan = await dbContext.StudyPlans
            .FirstOrDefaultAsync(
                x => x.Id == command.StudyPlanId && x.UserId == userId,
                cancellationToken);

        if (plan is null)
            throw new AppNotFoundException(
                "Study plan was not found.",
                "study_plan_not_found");

        if (plan.Status == PlanStatus.Cancelled)
            throw new AppConflictException(
                "Cancelled plan cannot be resumed.",
                "cancelled_plan_cannot_be_resumed");

        if (plan.Status == PlanStatus.Completed)
            throw new AppConflictException(
                "Completed plan cannot be resumed.",
                "completed_plan_cannot_be_resumed");

        plan.Status = PlanStatus.Active;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return 1;
    }
}