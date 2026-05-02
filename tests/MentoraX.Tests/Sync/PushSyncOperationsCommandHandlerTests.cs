using System.Text.Json;
using MentoraX.Application.Abstractions.Scheduling;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Sync;
using MentoraX.Application.Features.Sync.Commands;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
using MentoraX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MentoraX.Tests.Sync;

public sealed class PushSyncOperationsCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Handle_AppliesSessionStartedOperation_AndRecordsIt()
    {
        await using var dbContext = CreateDbContext();
        var ids = await SeedActiveStudyPlanAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var startedAtUtc = DateTime.UtcNow.AddMinutes(-15);

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "start-session-1",
                    operationType: SyncContract.OperationTypes.StudySessionStarted,
                    entityType: SyncContract.EntityTypes.StudySession,
                    entityId: ids.SessionId,
                    occurredAtUtc: startedAtUtc,
                    payload: new
                    {
                        sessionId = ids.SessionId,
                        startedAtUtc
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);

        var session = await dbContext.StudySessions
            .Include(x => x.StudyPlanItem)
            .SingleAsync(x => x.Id == ids.SessionId);
        var recordedOperation = await dbContext.SyncOperations.SingleAsync();

        Assert.Equal(startedAtUtc, session.StartedAtUtc);
        Assert.Equal(StudyPlanItemStatus.InProgress, session.StudyPlanItem?.Status);
        Assert.Equal("start-session-1", recordedOperation.OperationId);
        Assert.Equal(SyncContract.ResultStatuses.Applied, recordedOperation.Status);
        Assert.Null(recordedOperation.Error);
    }

    [Fact]
    public async Task Handle_AppliesSessionCompletedOperation_AndCreatesNextReviewSession()
    {
        await using var dbContext = CreateDbContext();
        var ids = await SeedActiveStudyPlanAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var completedAtUtc = DateTime.UtcNow;

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "complete-session-1",
                    operationType: SyncContract.OperationTypes.StudySessionCompleted,
                    entityType: SyncContract.EntityTypes.StudySession,
                    entityId: ids.SessionId,
                    occurredAtUtc: completedAtUtc,
                    payload: new
                    {
                        sessionId = ids.SessionId,
                        qualityScore = 5,
                        difficultyScore = 2,
                        actualDurationMinutes = 25,
                        reviewNotes = "offline completion",
                        completedAtUtc
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);

        var originalSession = await dbContext.StudySessions
            .Include(x => x.StudyPlanItem)
            .SingleAsync(x => x.Id == ids.SessionId);
        var progress = await dbContext.StudyProgresses.SingleAsync(x => x.Id == ids.ProgressId);
        var repetitionItem = await dbContext.StudyPlanItems
            .SingleAsync(x => x.StudyPlanId == ids.PlanId && x.ItemType == StudyItemType.Repetition);
        var sessions = await dbContext.StudySessions
            .Where(x => x.StudyPlanId == ids.PlanId)
            .OrderBy(x => x.Order)
            .ToListAsync();

        Assert.True(originalSession.IsCompleted);
        Assert.Equal(completedAtUtc, originalSession.CompletedAtUtc);
        Assert.Equal(5, originalSession.QualityScore);
        Assert.Equal(2, originalSession.DifficultyScore);
        Assert.Equal(25, originalSession.ActualDurationMinutes);
        Assert.Equal("offline completion", originalSession.ReviewNotes);
        Assert.Equal(StudyPlanItemStatus.Completed, originalSession.StudyPlanItem?.Status);
        Assert.Equal(2, progress.RepetitionCount);
        Assert.Equal(3, progress.IntervalDays);
        Assert.Equal(2.7, progress.EasinessFactor);
        Assert.Equal(completedAtUtc.AddDays(3), progress.NextReviewAtUtc);
        Assert.Equal(2, sessions.Count);
        Assert.Equal(repetitionItem.Id, sessions.Last().StudyPlanItemId);
        Assert.False(sessions.Last().IsCompleted);
    }

    [Fact]
    public async Task Handle_ReturnsAlreadyApplied_WhenOperationIsReplayed()
    {
        await using var dbContext = CreateDbContext();
        var ids = await SeedActiveStudyPlanAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var operation = Operation(
            operationId: "pause-plan-1",
            operationType: SyncContract.OperationTypes.StudyPlanPaused,
            entityType: SyncContract.EntityTypes.StudyPlan,
            entityId: ids.PlanId,
            occurredAtUtc: DateTime.UtcNow,
            payload: new { planId = ids.PlanId });

        var firstResponse = await handler.Handle(
            new PushSyncOperationsCommand([operation]),
            CancellationToken.None);
        var replayResponse = await handler.Handle(
            new PushSyncOperationsCommand([operation]),
            CancellationToken.None);

        var firstResult = Assert.Single(firstResponse.Results);
        var replayResult = Assert.Single(replayResponse.Results);
        var recordedOperations = await dbContext.SyncOperations.ToListAsync();
        var plan = await dbContext.StudyPlans.SingleAsync(x => x.Id == ids.PlanId);

        Assert.Equal(SyncContract.ResultStatuses.Applied, firstResult.Status);
        Assert.Equal(SyncContract.ResultStatuses.AlreadyApplied, replayResult.Status);
        Assert.Single(recordedOperations);
        Assert.Equal(PlanStatus.Paused, plan.Status);
    }

    [Fact]
    public async Task Handle_AppliesStudyPlanCompletedOperation_AndCancelsIncompleteItems()
    {
        await using var dbContext = CreateDbContext();
        var ids = await SeedActiveStudyPlanAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var completedAtUtc = DateTime.UtcNow;

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "complete-plan-1",
                    operationType: SyncContract.OperationTypes.StudyPlanCompleted,
                    entityType: SyncContract.EntityTypes.StudyPlan,
                    entityId: ids.PlanId,
                    occurredAtUtc: completedAtUtc,
                    payload: new { planId = ids.PlanId })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var plan = await dbContext.StudyPlans.SingleAsync(x => x.Id == ids.PlanId);
        var item = await dbContext.StudyPlanItems.SingleAsync(x => x.Id == ids.ItemId);
        var session = await dbContext.StudySessions.SingleAsync(x => x.Id == ids.SessionId);

        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);
        Assert.Equal(PlanStatus.Completed, plan.Status);
        Assert.Equal(completedAtUtc, plan.UpdatedAtUtc);
        Assert.Equal(StudyPlanItemStatus.Cancelled, item.Status);
        Assert.Equal(completedAtUtc, session.UpdatedAtUtc);
        Assert.False(session.IsCompleted);
    }

    [Fact]
    public async Task Handle_RecordsConflict_WhenCompletedPlanIsCancelled()
    {
        await using var dbContext = CreateDbContext();
        var ids = await SeedActiveStudyPlanAsync(dbContext);
        var plan = await dbContext.StudyPlans.SingleAsync(x => x.Id == ids.PlanId);
        plan.Status = PlanStatus.Completed;
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "cancel-plan-1",
                    operationType: SyncContract.OperationTypes.StudyPlanCancelled,
                    entityType: SyncContract.EntityTypes.StudyPlan,
                    entityId: ids.PlanId,
                    occurredAtUtc: DateTime.UtcNow,
                    payload: new { planId = ids.PlanId })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var recordedOperation = await dbContext.SyncOperations.SingleAsync();

        Assert.Equal(SyncContract.ResultStatuses.Conflict, result.Status);
        Assert.StartsWith(
            $"{SyncContract.ErrorCodes.CompletedPlanCannotBeCancelled}:",
            result.Error);
        Assert.Equal(SyncContract.ResultStatuses.Conflict, recordedOperation.Status);
        Assert.StartsWith(
            $"{SyncContract.ErrorCodes.CompletedPlanCannotBeCancelled}:",
            recordedOperation.Error);
    }

    [Fact]
    public async Task Handle_RecordsConflict_ForUnsupportedOperationType()
    {
        await using var dbContext = CreateDbContext();
        var ids = await SeedActiveStudyPlanAsync(dbContext);
        var handler = CreateHandler(dbContext);

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "unsupported-1",
                    operationType: "UnsupportedOperation",
                    entityType: SyncContract.EntityTypes.StudyPlan,
                    entityId: ids.PlanId,
                    occurredAtUtc: DateTime.UtcNow,
                    payload: new { planId = ids.PlanId })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);

        Assert.Equal(SyncContract.ResultStatuses.Conflict, result.Status);
        Assert.StartsWith(
            $"{SyncContract.ErrorCodes.SyncOperationNotSupported}:",
            result.Error);
    }

    private static MentoraXDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MentoraXDbContext>()
            .UseInMemoryDatabase($"mentorax-sync-tests-{Guid.NewGuid()}")
            .Options;

        return new MentoraXDbContext(options);
    }

    private static PushSyncOperationsCommandHandler CreateHandler(
        MentoraXDbContext dbContext)
    {
        return new PushSyncOperationsCommandHandler(
            dbContext,
            new FakeStudyScheduleEngine(),
            new FakeCurrentUserService(UserId));
    }

    private static async Task<SeedIds> SeedActiveStudyPlanAsync(
        MentoraXDbContext dbContext)
    {
        var materialId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new User(
            "Sync Test User",
            "sync-test@mentorax.local",
            "password-hash")
        {
            Id = UserId
        };
        var material = new LearningMaterial(
            UserId,
            "Sync Material",
            MaterialType.Text,
            "Sync material content",
            30)
        {
            Id = materialId
        };
        var plan = new StudyPlan(
            UserId,
            materialId,
            "Sync Plan",
            DateOnly.FromDateTime(now),
            30)
        {
            Id = planId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var chunk = new MaterialChunk(
            materialId,
            1,
            "Chunk content",
            "Chunk title",
            null,
            null,
            2,
            30);
        var item = new StudyPlanItem(
            planId,
            chunk.Id,
            "Study item",
            null,
            StudyItemType.NewStudy,
            1,
            now,
            null,
            null,
            30,
            1,
            true,
            null);
        var progress = new StudyProgress
        {
            Id = progressId,
            UserId = UserId,
            LearningMaterialId = materialId,
            StudyPlanId = planId,
            RepetitionCount = 1,
            IntervalDays = 1,
            EasinessFactor = 2.5,
            NextReviewAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var session = new StudySession
        {
            Id = sessionId,
            StudyPlanId = planId,
            StudyPlanItemId = item.Id,
            LearningMaterialId = materialId,
            UserId = UserId,
            StudyProgressId = progressId,
            ScheduledAtUtc = now,
            IsCompleted = false,
            Order = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Users.Add(user);
        dbContext.LearningMaterials.Add(material);
        dbContext.StudyPlans.Add(plan);
        dbContext.MaterialChunks.Add(chunk);
        dbContext.StudyPlanItems.Add(item);
        dbContext.StudyProgresses.Add(progress);
        dbContext.StudySessions.Add(session);
        await dbContext.SaveChangesAsync();

        return new SeedIds(materialId, planId, item.Id, sessionId, progressId);
    }

    private static SyncPushOperationDto Operation(
        string operationId,
        string operationType,
        string entityType,
        Guid entityId,
        DateTime occurredAtUtc,
        object payload)
    {
        return new SyncPushOperationDto(
            operationId,
            operationType,
            entityType,
            entityId,
            occurredAtUtc,
            JsonSerializer.Serialize(payload));
    }

    private sealed record SeedIds(
        Guid MaterialId,
        Guid PlanId,
        Guid ItemId,
        Guid SessionId,
        Guid ProgressId);

    private sealed class FakeCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid GetRequiredUserId() => userId;

        public Guid? GetUserId() => userId;

        public string? GetEmail() => "sync-test@mentorax.local";
    }

    private sealed class FakeStudyScheduleEngine : IStudyScheduleEngine
    {
        public StudyScheduleResult CalculateNext(
            int repetitionCount,
            int previousIntervalDays,
            double easinessFactor,
            int qualityScore,
            int difficultyScore,
            DateTime reviewedAtUtc)
        {
            return new StudyScheduleResult(
                RepetitionCount: 2,
                IntervalDays: 3,
                EasinessFactor: 2.7,
                NextReviewAtUtc: reviewedAtUtc.AddDays(3),
                SuccessStreakDelta: 1,
                IsFailure: false);
        }
    }
}
