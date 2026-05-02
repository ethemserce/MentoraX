using System.Globalization;
using System.Text.Json;
using MentoraX.Application.Abstractions.Persistence;
using MentoraX.Application.Abstractions.Scheduling;
using MentoraX.Application.Abstractions.Services;
using MentoraX.Application.Common;
using MentoraX.Application.Common.Exceptions;
using MentoraX.Application.DTOs;
using MentoraX.Application.Features.Sync;
using MentoraX.Domain.Entities;
using MentoraX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MentoraX.Application.Features.Sync.Commands;

public sealed record PushSyncOperationsCommand(IReadOnlyCollection<SyncPushOperationDto> Operations)
    : ICommand<SyncPushResponseDto>;

public sealed class PushSyncOperationsCommandHandler(
    IApplicationDbContext dbContext,
    IStudyScheduleEngine scheduleEngine,
    ICurrentUserService currentUserService)
    : ICommandHandler<PushSyncOperationsCommand, SyncPushResponseDto>
{
    private const string StatusApplied = SyncContract.ResultStatuses.Applied;
    private const string StatusAlreadyApplied = SyncContract.ResultStatuses.AlreadyApplied;
    private const string StatusConflict = SyncContract.ResultStatuses.Conflict;
    private const string StatusFailed = SyncContract.ResultStatuses.Failed;

    public async Task<SyncPushResponseDto> Handle(
        PushSyncOperationsCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var results = new List<SyncPushOperationResultDto>();

        foreach (var operation in command.Operations)
        {
            results.Add(await HandleOperationAsync(userId, operation, cancellationToken));
        }

        return new SyncPushResponseDto(DateTime.UtcNow, results);
    }

    private async Task<SyncPushOperationResultDto> HandleOperationAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operation.OperationId))
        {
            return new SyncPushOperationResultDto(
                operation.OperationId,
                StatusFailed,
                $"{SyncContract.ErrorCodes.SyncOperationIdRequired}: OperationId is required.");
        }

        var existingOperation = await dbContext.SyncOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.OperationId == operation.OperationId,
                cancellationToken);

        if (existingOperation is not null)
        {
            var status = existingOperation.Status == StatusApplied
                ? StatusAlreadyApplied
                : existingOperation.Status;

            return new SyncPushOperationResultDto(
                operation.OperationId,
                status,
                existingOperation.Error);
        }

        try
        {
            switch (operation.OperationType)
            {
                case SyncContract.OperationTypes.StudySessionStarted:
                    await ApplyStudySessionStartedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.StudySessionCompleted:
                    await ApplyStudySessionCompletedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.StudyPlanPaused:
                    await ApplyStudyPlanPausedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.StudyPlanResumed:
                    await ApplyStudyPlanResumedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.StudyPlanCancelled:
                    await ApplyStudyPlanCancelledAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.StudyPlanCompleted:
                    await ApplyStudyPlanCompletedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.MaterialCreated:
                    await ApplyMaterialCreatedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.MaterialChunkCreated:
                    await ApplyMaterialChunkCreatedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.MaterialChunkUpdated:
                    await ApplyMaterialChunkUpdatedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.MaterialChunkDeleted:
                    await ApplyMaterialChunkDeletedAsync(userId, operation, cancellationToken);
                    break;
                case SyncContract.OperationTypes.MaterialChunksReordered:
                    await ApplyMaterialChunksReorderedAsync(userId, operation, cancellationToken);
                    break;
                default:
                    return await RecordOperationAsync(
                        userId,
                        operation,
                        StatusConflict,
                        $"{SyncContract.ErrorCodes.SyncOperationNotSupported}: {operation.OperationType} is not supported.",
                        cancellationToken);
            }

            return await RecordOperationAsync(
                userId,
                operation,
                StatusApplied,
                null,
                cancellationToken);
        }
        catch (AppException ex)
        {
            return await RecordOperationAsync(
                userId,
                operation,
                ResolveFailureStatus(ex),
                $"{ex.Code}: {ex.Message}",
                cancellationToken);
        }
    }

    private async Task ApplyStudySessionStartedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var sessionId = ReadSessionId(operation, root);
        var startedAtUtc = ReadOptionalDateTime(root, "startedAtUtc") ??
                           operation.OccurredAtUtc ??
                           DateTime.UtcNow;

        var session = await dbContext.StudySessions
            .Include(x => x.StudyPlan)
            .Include(x => x.StudyPlanItem)
            .FirstOrDefaultAsync(
                x => x.Id == sessionId &&
                     x.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            throw new AppNotFoundException(
                "Study session not found.",
                SyncContract.ErrorCodes.StudySessionNotFound);
        }

        if (session.IsCompleted)
            return;

        if (session.StudyPlan is null)
        {
            throw new AppConflictException(
                "Session does not belong to a valid study plan.",
                SyncContract.ErrorCodes.SessionPlanNotFound);
        }

        if (session.StudyPlan.Status != PlanStatus.Active)
        {
            throw new AppConflictException(
                "This plan is no longer active. Please refresh the page.",
                SyncContract.ErrorCodes.StudyPlanNotActive);
        }

        if (!session.StartedAtUtc.HasValue)
        {
            session.MarkStarted(startedAtUtc);
        }

        if (session.StudyPlanItem is not null)
        {
            session.StudyPlanItem.MarkInProgress();
        }
    }

    private async Task ApplyStudySessionCompletedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var sessionId = ReadSessionId(operation, root);
        var qualityScore = ReadRequiredInt(root, "qualityScore");
        var difficultyScore = ReadRequiredInt(root, "difficultyScore");
        var actualDurationMinutes = ReadRequiredInt(root, "actualDurationMinutes");
        var reviewNotes = ReadOptionalString(root, "reviewNotes");
        var completedAtUtc = ReadOptionalDateTime(root, "completedAtUtc") ??
                             operation.OccurredAtUtc ??
                             DateTime.UtcNow;

        var session = await dbContext.StudySessions
            .Include(x => x.StudyProgress)
            .Include(x => x.StudyPlan)
            .Include(x => x.StudyPlanItem)
            .FirstOrDefaultAsync(
                x => x.Id == sessionId &&
                     x.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            throw new AppNotFoundException(
                "Study session not found.",
                SyncContract.ErrorCodes.StudySessionNotFound);
        }

        if (session.IsCompleted)
            return;

        if (session.StudyPlan is null)
        {
            throw new AppConflictException(
                "Session does not belong to a valid study plan.",
                SyncContract.ErrorCodes.SessionPlanNotFound);
        }

        if (session.StudyPlan.Status != PlanStatus.Active)
        {
            throw new AppConflictException(
                "This plan is no longer active. Please refresh the page.",
                SyncContract.ErrorCodes.StudyPlanNotActive);
        }

        if (!session.StartedAtUtc.HasValue)
        {
            var inferredStartUtc = completedAtUtc.AddMinutes(-Math.Max(actualDurationMinutes, 0));
            session.MarkStarted(inferredStartUtc);
        }

        session.MarkCompleted(
            qualityScore,
            difficultyScore,
            actualDurationMinutes,
            reviewNotes,
            completedAtUtc);

        if (session.StudyPlanItem is not null)
        {
            session.StudyPlanItem.MarkCompleted();
        }

        var progress = session.StudyProgress;

        var progressResult = scheduleEngine.CalculateNext(
            progress.RepetitionCount,
            progress.IntervalDays,
            progress.EasinessFactor,
            qualityScore,
            difficultyScore,
            completedAtUtc);

        progress.RepetitionCount = progressResult.RepetitionCount;
        progress.IntervalDays = progressResult.IntervalDays;
        progress.EasinessFactor = progressResult.EasinessFactor;
        progress.LastReviewedAtUtc = completedAtUtc;
        progress.NextReviewAtUtc = progressResult.NextReviewAtUtc;
        progress.UpdatedAtUtc = completedAtUtc;

        if (progressResult.IsFailure)
        {
            progress.FailureCount += 1;
            progress.SuccessStreak = 0;
        }
        else
        {
            progress.SuccessStreak += progressResult.SuccessStreakDelta;
        }

        var maxItemOrder = await dbContext.StudyPlanItems
            .Where(x => x.StudyPlanId == session.StudyPlanId)
            .Select(x => (int?)x.OrderNo)
            .MaxAsync(cancellationToken) ?? 0;

        var maxSessionOrder = await dbContext.StudySessions
            .Where(x => x.StudyPlanId == session.StudyPlanId)
            .Select(x => (int?)x.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var nextPlanItem = new StudyPlanItem(
            studyPlanId: session.StudyPlanId,
            materialChunkId: session.StudyPlanItem?.MaterialChunkId,
            title: session.StudyPlanItem?.Title ?? session.StudyPlan.Title,
            description: session.StudyPlanItem?.Description,
            itemType: StudyItemType.Repetition,
            orderNo: maxItemOrder + 1,
            plannedDateUtc: progress.NextReviewAtUtc,
            plannedStartTime: null,
            plannedEndTime: null,
            durationMinutes: session.StudyPlan.DailyTargetMinutes,
            priority: 1,
            isMandatory: true,
            sourceReason: "Generated from sync repetition schedule");

        var nextSession = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyPlanId = session.StudyPlanId,
            StudyPlanItemId = nextPlanItem.Id,
            LearningMaterialId = session.LearningMaterialId,
            UserId = session.UserId,
            StudyProgressId = progress.Id,
            ScheduledAtUtc = progress.NextReviewAtUtc,
            IsCompleted = false,
            Order = maxSessionOrder + 1,
            CreatedAtUtc = completedAtUtc,
            UpdatedAtUtc = completedAtUtc
        };

        dbContext.StudyPlanItems.Add(nextPlanItem);
        dbContext.StudySessions.Add(nextSession);
    }

    private async Task ApplyStudyPlanPausedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var planId = ReadPlanId(operation, payload.RootElement);
        var occurredAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        var plan = await dbContext.StudyPlans
            .FirstOrDefaultAsync(
                x => x.Id == planId &&
                     x.UserId == userId,
                cancellationToken);

        if (plan is null)
        {
            throw new AppNotFoundException(
                "Study plan was not found.",
                SyncContract.ErrorCodes.StudyPlanNotFound);
        }

        plan.Status = PlanStatus.Paused;
        plan.UpdatedAtUtc = occurredAtUtc;
    }

    private async Task ApplyStudyPlanResumedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var planId = ReadPlanId(operation, payload.RootElement);
        var occurredAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        var plan = await dbContext.StudyPlans
            .FirstOrDefaultAsync(
                x => x.Id == planId &&
                     x.UserId == userId,
                cancellationToken);

        if (plan is null)
        {
            throw new AppNotFoundException(
                "Study plan was not found.",
                SyncContract.ErrorCodes.StudyPlanNotFound);
        }

        if (plan.Status == PlanStatus.Cancelled)
        {
            throw new AppConflictException(
                "Cancelled plan cannot be resumed.",
                SyncContract.ErrorCodes.CancelledPlanCannotBeResumed);
        }

        if (plan.Status == PlanStatus.Completed)
        {
            throw new AppConflictException(
                "Completed plan cannot be resumed.",
                SyncContract.ErrorCodes.CompletedPlanCannotBeResumed);
        }

        plan.Status = PlanStatus.Active;
        plan.UpdatedAtUtc = occurredAtUtc;
    }

    private async Task ApplyStudyPlanCancelledAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var planId = ReadPlanId(operation, payload.RootElement);
        var occurredAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        var plan = await dbContext.StudyPlans
            .Include(x => x.Items)
                .ThenInclude(x => x.StudySessions)
            .FirstOrDefaultAsync(
                x => x.Id == planId &&
                     x.UserId == userId,
                cancellationToken);

        if (plan is null)
        {
            throw new AppNotFoundException(
                "Study plan was not found.",
                SyncContract.ErrorCodes.StudyPlanNotFound);
        }

        if (plan.Status == PlanStatus.Completed)
        {
            throw new AppConflictException(
                "Completed plan cannot be cancelled.",
                SyncContract.ErrorCodes.CompletedPlanCannotBeCancelled);
        }

        plan.Status = PlanStatus.Cancelled;
        plan.UpdatedAtUtc = occurredAtUtc;

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
                    session.UpdatedAtUtc = occurredAtUtc;
                }
            }
        }
    }

    private async Task ApplyStudyPlanCompletedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var planId = ReadPlanId(operation, payload.RootElement);
        var occurredAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        var plan = await dbContext.StudyPlans
            .Include(x => x.Items)
                .ThenInclude(x => x.StudySessions)
            .FirstOrDefaultAsync(
                x => x.Id == planId &&
                     x.UserId == userId,
                cancellationToken);

        if (plan is null)
        {
            throw new AppNotFoundException(
                "Study plan was not found.",
                SyncContract.ErrorCodes.StudyPlanNotFound);
        }

        if (plan.Status == PlanStatus.Cancelled)
        {
            throw new AppConflictException(
                "Cancelled plan cannot be completed.",
                SyncContract.ErrorCodes.CancelledPlanCannotBeCompleted);
        }

        if (plan.Status == PlanStatus.Completed)
        {
            return;
        }

        plan.Status = PlanStatus.Completed;
        plan.UpdatedAtUtc = occurredAtUtc;

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
                    session.UpdatedAtUtc = occurredAtUtc;
                }
            }
        }
    }

    private async Task ApplyMaterialCreatedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var materialId = ReadMaterialId(operation, root);
        var occurredAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        var existingMaterial = await dbContext.LearningMaterials
            .FirstOrDefaultAsync(x => x.Id == materialId, cancellationToken);

        if (existingMaterial is not null)
        {
            if (existingMaterial.UserId != userId)
            {
                throw new AppConflictException(
                    "Material id is already used by another user.",
                    SyncContract.ErrorCodes.MaterialIdConflict);
            }

            return;
        }

        var title = ReadRequiredString(root, "title");
        var materialTypeRaw = ReadRequiredString(root, "materialType");
        var content = ReadRequiredString(root, "content");
        var estimatedDurationMinutes = ReadRequiredInt(root, "estimatedDurationMinutes");
        var description = ReadOptionalString(root, "description");
        var tags = ReadOptionalString(root, "tags");

        if (!Enum.TryParse<MaterialType>(materialTypeRaw, true, out var materialType))
        {
            throw new AppConflictException(
                "Sync payload field materialType must be a valid material type.",
                SyncContract.ErrorCodes.SyncPayloadFieldInvalid);
        }

        if (estimatedDurationMinutes <= 0)
        {
            throw new AppConflictException(
                "Estimated duration minutes must be greater than zero.",
                SyncContract.ErrorCodes.InvalidEstimatedStudyMinutes);
        }

        var material = new LearningMaterial(
            userId,
            title,
            materialType,
            content,
            estimatedDurationMinutes,
            description,
            tags)
        {
            Id = materialId,
            CreatedAtUtc = occurredAtUtc,
            UpdatedAtUtc = occurredAtUtc
        };

        dbContext.LearningMaterials.Add(material);

        var defaultChunkId =
            TryReadGuid(root, "defaultChunkId", out var parsedDefaultChunkId)
                ? parsedDefaultChunkId
                : TryReadGuid(root, "chunkId", out var parsedChunkId)
                ? parsedChunkId
                : Guid.NewGuid();

        var defaultChunk = new MaterialChunk(
            learningMaterialId: materialId,
            orderNo: 1,
            content: content,
            title: title,
            summary: description,
            keywords: tags,
            difficultyLevel: 1,
            estimatedStudyMinutes: estimatedDurationMinutes,
            isGeneratedByAI: false,
            id: defaultChunkId)
        {
            CreatedAtUtc = occurredAtUtc,
            UpdatedAtUtc = occurredAtUtc
        };

        dbContext.MaterialChunks.Add(defaultChunk);
    }

    private async Task ApplyMaterialChunkCreatedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var materialId = ReadMaterialIdFromPayload(root);
        var chunkId = ReadChunkId(operation, root);
        var occurredAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        await EnsureMaterialExistsAsync(userId, materialId, cancellationToken);

        var existingChunk = await dbContext.MaterialChunks
            .Include(x => x.LearningMaterial)
            .FirstOrDefaultAsync(x => x.Id == chunkId, cancellationToken);

        if (existingChunk is not null)
        {
            if (existingChunk.LearningMaterialId == materialId &&
                existingChunk.LearningMaterial.UserId == userId)
            {
                return;
            }

            throw new AppConflictException(
                "Material chunk id is already used.",
                SyncContract.ErrorCodes.MaterialChunkIdConflict);
        }

        var title = ReadOptionalString(root, "title");
        var content = ReadRequiredString(root, "content");
        var summary = ReadOptionalString(root, "summary");
        var keywords = ReadOptionalString(root, "keywords");
        var difficultyLevel = ReadRequiredInt(root, "difficultyLevel");
        var estimatedStudyMinutes = ReadRequiredInt(root, "estimatedStudyMinutes");

        ValidateChunkPayload(content, difficultyLevel, estimatedStudyMinutes);

        var nextOrderNo = ReadOptionalInt(root, "orderNo") ??
            ((await dbContext.MaterialChunks
                .Where(x => x.LearningMaterialId == materialId)
                .Select(x => (int?)x.OrderNo)
                .MaxAsync(cancellationToken)) ?? 0) + 1;

        var chunk = new MaterialChunk(
            learningMaterialId: materialId,
            orderNo: nextOrderNo,
            content: content,
            title: title,
            summary: summary,
            keywords: keywords,
            difficultyLevel: difficultyLevel,
            estimatedStudyMinutes: estimatedStudyMinutes,
            isGeneratedByAI: false,
            id: chunkId)
        {
            CreatedAtUtc = occurredAtUtc,
            UpdatedAtUtc = occurredAtUtc
        };

        dbContext.MaterialChunks.Add(chunk);
    }

    private async Task ApplyMaterialChunkUpdatedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var materialId = ReadMaterialIdFromPayload(root);
        var chunkId = ReadChunkId(operation, root);

        var chunk = await dbContext.MaterialChunks
            .Include(x => x.LearningMaterial)
            .FirstOrDefaultAsync(
                x => x.Id == chunkId &&
                     x.LearningMaterialId == materialId &&
                     x.LearningMaterial.UserId == userId,
                cancellationToken);

        if (chunk is null)
        {
            throw new AppNotFoundException(
                "Material chunk was not found.",
                SyncContract.ErrorCodes.MaterialChunkNotFound);
        }

        var title = ReadOptionalString(root, "title");
        var content = ReadRequiredString(root, "content");
        var summary = ReadOptionalString(root, "summary");
        var keywords = ReadOptionalString(root, "keywords");
        var difficultyLevel = ReadRequiredInt(root, "difficultyLevel");
        var estimatedStudyMinutes = ReadRequiredInt(root, "estimatedStudyMinutes");

        ValidateChunkPayload(content, difficultyLevel, estimatedStudyMinutes);

        chunk.Update(
            content: content,
            title: title,
            summary: summary,
            keywords: keywords,
            difficultyLevel: difficultyLevel,
            estimatedStudyMinutes: estimatedStudyMinutes);

        if (operation.OccurredAtUtc.HasValue)
        {
            chunk.UpdatedAtUtc = operation.OccurredAtUtc.Value;
        }
    }

    private async Task ApplyMaterialChunkDeletedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var materialId = ReadMaterialIdFromPayload(root);
        var chunkId = ReadChunkId(operation, root);
        var deletedAtUtc = operation.OccurredAtUtc ?? DateTime.UtcNow;

        var chunk = await dbContext.MaterialChunks
            .Include(x => x.LearningMaterial)
            .FirstOrDefaultAsync(
                x => x.Id == chunkId &&
                     x.LearningMaterialId == materialId &&
                     x.LearningMaterial.UserId == userId,
                cancellationToken);

        if (chunk is null)
        {
            throw new AppNotFoundException(
                "Material chunk was not found.",
                SyncContract.ErrorCodes.MaterialChunkNotFound);
        }

        var isUsedInPlan = await dbContext.StudyPlanItems
            .AnyAsync(x => x.MaterialChunkId == chunk.Id, cancellationToken);

        if (isUsedInPlan)
        {
            throw new AppConflictException(
                "This chunk is used in a study plan and cannot be deleted.",
                SyncContract.ErrorCodes.ChunkIsUsedInStudyPlan);
        }

        dbContext.SyncTombstones.Add(new SyncTombstone(
            userId,
            SyncContract.EntityTypes.MaterialChunk,
            chunk.Id,
            deletedAtUtc,
            "{}"));

        chunk.LearningMaterial.Touch();
        chunk.LearningMaterial.UpdatedAtUtc = deletedAtUtc;
        dbContext.MaterialChunks.Remove(chunk);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ReorderRemainingChunksAsync(materialId, cancellationToken);
    }

    private async Task ApplyMaterialChunksReorderedAsync(
        Guid userId,
        SyncPushOperationDto operation,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(operation.Payload);
        var root = payload.RootElement;
        var materialId = ReadMaterialId(operation, root);
        var chunkIds = ReadRequiredGuidList(root, "chunkIds");

        await EnsureMaterialExistsAsync(userId, materialId, cancellationToken);

        if (chunkIds.Count == 0)
        {
            throw new AppConflictException(
                "Chunk order list cannot be empty.",
                SyncContract.ErrorCodes.ChunkOrderRequired);
        }

        var requestedIds = chunkIds.Distinct().ToList();
        var chunks = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == materialId)
            .ToListAsync(cancellationToken);

        if (chunks.Count != requestedIds.Count)
        {
            throw new AppConflictException(
                "Chunk order list must contain all chunks for this material.",
                SyncContract.ErrorCodes.InvalidChunkOrderCount);
        }

        var existingIds = chunks.Select(x => x.Id).ToHashSet();

        if (requestedIds.Any(id => !existingIds.Contains(id)))
        {
            throw new AppConflictException(
                "Chunk order list contains invalid chunk ids.",
                SyncContract.ErrorCodes.InvalidChunkOrderIds);
        }

        await ApplyChunkOrderAsync(chunks, requestedIds, cancellationToken);
    }

    private async Task EnsureMaterialExistsAsync(
        Guid userId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var materialExists = await dbContext.LearningMaterials
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == materialId &&
                     x.UserId == userId,
                cancellationToken);

        if (!materialExists)
        {
            throw new AppNotFoundException(
                "Learning material was not found.",
                SyncContract.ErrorCodes.LearningMaterialNotFound);
        }
    }

    private async Task ReorderRemainingChunksAsync(
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var remainingChunks = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == materialId)
            .OrderBy(x => x.OrderNo)
            .ToListAsync(cancellationToken);

        var requestedIds = remainingChunks.Select(x => x.Id).ToList();
        await ApplyChunkOrderAsync(remainingChunks, requestedIds, cancellationToken);
    }

    private async Task ApplyChunkOrderAsync(
        IReadOnlyCollection<MaterialChunk> chunks,
        IReadOnlyList<Guid> requestedIds,
        CancellationToken cancellationToken)
    {
        var chunkList = chunks.ToList();

        for (var i = 0; i < chunkList.Count; i++)
        {
            chunkList[i].ChangeOrderTemporary(-(i + 1));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < requestedIds.Count; i++)
        {
            var chunk = chunkList.First(x => x.Id == requestedIds[i]);
            chunk.ChangeOrder(i + 1);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateChunkPayload(
        string content,
        int difficultyLevel,
        int estimatedStudyMinutes)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new AppConflictException(
                "Chunk content cannot be empty.",
                SyncContract.ErrorCodes.ChunkContentRequired);
        }

        if (difficultyLevel < 1 || difficultyLevel > 5)
        {
            throw new AppConflictException(
                "Difficulty level must be between 1 and 5.",
                SyncContract.ErrorCodes.InvalidDifficultyLevel);
        }

        if (estimatedStudyMinutes <= 0)
        {
            throw new AppConflictException(
                "Estimated study minutes must be greater than zero.",
                SyncContract.ErrorCodes.InvalidEstimatedStudyMinutes);
        }
    }

    private async Task<SyncPushOperationResultDto> RecordOperationAsync(
        Guid userId,
        SyncPushOperationDto operation,
        string status,
        string? error,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        dbContext.SyncOperations.Add(new SyncOperation(
            userId,
            operation.OperationId,
            operation.OperationType,
            operation.EntityType,
            operation.EntityId,
            operation.Payload,
            operation.OccurredAtUtc ?? now,
            now,
            status,
            error));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SyncPushOperationResultDto(
            operation.OperationId,
            status,
            error);
    }

    private static string ResolveFailureStatus(AppException exception)
    {
        return exception is AppConflictException ||
               exception is AppNotFoundException ||
               IsTerminalSyncError(exception.Code)
            ? StatusConflict
            : StatusFailed;
    }

    private static bool IsTerminalSyncError(string code)
    {
        return code is
            SyncContract.ErrorCodes.SyncInvalidPayload or
            SyncContract.ErrorCodes.SyncSessionIdRequired or
            SyncContract.ErrorCodes.SyncPlanIdRequired or
            SyncContract.ErrorCodes.SyncMaterialIdRequired or
            SyncContract.ErrorCodes.SyncChunkIdRequired or
            SyncContract.ErrorCodes.SyncPayloadFieldRequired or
            SyncContract.ErrorCodes.SyncPayloadFieldInvalid;
    }

    private static JsonDocument ParsePayload(string payload)
    {
        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        }
        catch (JsonException)
        {
            throw new AppConflictException(
                "Sync payload is not valid JSON.",
                SyncContract.ErrorCodes.SyncInvalidPayload);
        }
    }

    private static Guid ReadSessionId(SyncPushOperationDto operation, JsonElement root)
    {
        if (operation.EntityId != Guid.Empty)
            return operation.EntityId;

        if (TryReadGuid(root, "sessionId", out var sessionId))
            return sessionId;

        throw new AppConflictException(
            "Study session sync operation requires a sessionId.",
            SyncContract.ErrorCodes.SyncSessionIdRequired);
    }

    private static Guid ReadPlanId(SyncPushOperationDto operation, JsonElement root)
    {
        if (operation.EntityId != Guid.Empty)
            return operation.EntityId;

        if (TryReadGuid(root, "planId", out var planId))
            return planId;

        if (TryReadGuid(root, "studyPlanId", out var studyPlanId))
            return studyPlanId;

        throw new AppConflictException(
            "Study plan sync operation requires a planId.",
            SyncContract.ErrorCodes.SyncPlanIdRequired);
    }

    private static Guid ReadMaterialId(SyncPushOperationDto operation, JsonElement root)
    {
        if (operation.EntityId != Guid.Empty)
            return operation.EntityId;

        if (TryReadGuid(root, "materialId", out var materialId))
            return materialId;

        if (TryReadGuid(root, "learningMaterialId", out var learningMaterialId))
            return learningMaterialId;

        if (TryReadGuid(root, "id", out var id))
            return id;

        throw new AppConflictException(
            "Material sync operation requires a materialId.",
            SyncContract.ErrorCodes.SyncMaterialIdRequired);
    }

    private static Guid ReadMaterialIdFromPayload(JsonElement root)
    {
        if (TryReadGuid(root, "materialId", out var materialId))
            return materialId;

        if (TryReadGuid(root, "learningMaterialId", out var learningMaterialId))
            return learningMaterialId;

        throw new AppConflictException(
            "Material chunk sync operation requires a materialId.",
            SyncContract.ErrorCodes.SyncMaterialIdRequired);
    }

    private static Guid ReadChunkId(SyncPushOperationDto operation, JsonElement root)
    {
        if (operation.EntityId != Guid.Empty)
            return operation.EntityId;

        if (TryReadGuid(root, "chunkId", out var chunkId))
            return chunkId;

        if (TryReadGuid(root, "id", out var id))
            return id;

        throw new AppConflictException(
            "Material chunk sync operation requires a chunkId.",
            SyncContract.ErrorCodes.SyncChunkIdRequired);
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            throw new AppConflictException(
                $"Sync payload requires {propertyName}.",
                SyncContract.ErrorCodes.SyncPayloadFieldRequired);
        }

        var rawValue = value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.GetRawText();

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new AppConflictException(
                $"Sync payload requires {propertyName}.",
                SyncContract.ErrorCodes.SyncPayloadFieldRequired);
        }

        return rawValue.Trim();
    }

    private static int ReadRequiredInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            throw new AppConflictException(
                $"Sync payload requires {propertyName}.",
                SyncContract.ErrorCodes.SyncPayloadFieldRequired);
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        throw new AppConflictException(
            $"Sync payload field {propertyName} must be an integer.",
            SyncContract.ErrorCodes.SyncPayloadFieldInvalid);
    }

    private static int? ReadOptionalInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        throw new AppConflictException(
            $"Sync payload field {propertyName} must be an integer.",
            SyncContract.ErrorCodes.SyncPayloadFieldInvalid);
    }

    private static IReadOnlyList<Guid> ReadRequiredGuidList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            throw new AppConflictException(
                $"Sync payload requires {propertyName}.",
                SyncContract.ErrorCodes.SyncPayloadFieldRequired);
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new AppConflictException(
                $"Sync payload field {propertyName} must be a GUID array.",
                SyncContract.ErrorCodes.SyncPayloadFieldInvalid);
        }

        var ids = new List<Guid>();

        foreach (var item in value.EnumerateArray())
        {
            var rawValue = item.ValueKind == JsonValueKind.String
                ? item.GetString()
                : item.GetRawText();

            if (!Guid.TryParse(rawValue, out var id))
            {
                throw new AppConflictException(
                    $"Sync payload field {propertyName} must contain only GUID values.",
                    SyncContract.ErrorCodes.SyncPayloadFieldInvalid);
            }

            ids.Add(id);
        }

        return ids;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.GetRawText();
    }

    private static DateTime? ReadOptionalDateTime(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var rawValue = value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.GetRawText();

        if (DateTime.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dateTime))
        {
            return dateTime;
        }

        throw new AppConflictException(
            $"Sync payload field {propertyName} must be a UTC date-time.",
            SyncContract.ErrorCodes.SyncPayloadFieldInvalid);
    }

    private static bool TryReadGuid(JsonElement root, string propertyName, out Guid value)
    {
        value = Guid.Empty;

        if (!root.TryGetProperty(propertyName, out var element))
            return false;

        var rawValue = element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : element.GetRawText();

        return Guid.TryParse(rawValue, out value);
    }
}
