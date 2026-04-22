using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.StudyPlans.Commands;

public sealed record CancelStudyPlanCommand(Guid PlanId) : ICommand<int>;

public sealed class CancelStudyPlanCommandHandler(
    IApplicationDbContext dbContext)
    : ICommandHandler<CancelStudyPlanCommand, int>
{
    public async Task<int> Handle(CancelStudyPlanCommand command, CancellationToken ct)
    {
        var plan = await dbContext.StudyPlans
            .FirstOrDefaultAsync(x => x.Id == command.PlanId, ct);

        if (plan is null)
            throw new AppNotFoundException("Plan not found");

        plan.Cancel();

        return await dbContext.SaveChangesAsync(ct);
    }
}