using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record PauseStudyPlanCommand(Guid StudyPlanId) : ICommand<int>;

public sealed class PauseStudyPlanCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<PauseStudyPlanCommand, int>
{
    public async Task<int> Handle(
        PauseStudyPlanCommand command,
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

        plan.Status = PlanStatus.Paused;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return 1;
    }
}