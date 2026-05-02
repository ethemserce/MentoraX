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
