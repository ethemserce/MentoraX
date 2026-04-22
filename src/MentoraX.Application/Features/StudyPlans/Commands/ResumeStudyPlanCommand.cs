using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record ResumeStudyPlanCommand(Guid PlanId) : ICommand<int>;

public sealed class ResumeStudyPlanCommandHandler(
    IApplicationDbContext dbContext)
    : ICommandHandler<ResumeStudyPlanCommand, int>
{
    public async Task<int> Handle(ResumeStudyPlanCommand command, CancellationToken ct)
    {
        var plan = await dbContext.StudyPlans
            .FirstOrDefaultAsync(x => x.Id == command.PlanId, ct);

        if (plan is null)
            throw new AppNotFoundException("Plan not found");

        plan.Resume();

        return await dbContext.SaveChangesAsync(ct);
    }
}