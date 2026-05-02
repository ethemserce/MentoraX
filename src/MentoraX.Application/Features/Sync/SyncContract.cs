namespace MentoraX.Application.Features.Sync;

public static class SyncContract
{
    public static class OperationTypes
    {
        public const string StudySessionStarted = "StudySessionStarted";
        public const string StudySessionCompleted = "StudySessionCompleted";
        public const string StudyPlanPaused = "StudyPlanPaused";
        public const string StudyPlanResumed = "StudyPlanResumed";
        public const string StudyPlanCancelled = "StudyPlanCancelled";
        public const string StudyPlanCompleted = "StudyPlanCompleted";
    }

    public static class EntityTypes
    {
        public const string StudySession = "StudySession";
        public const string StudyPlan = "StudyPlan";
    }

    public static class ResultStatuses
    {
        public const string Applied = "applied";
        public const string AlreadyApplied = "already_applied";
        public const string Conflict = "conflict";
        public const string Failed = "failed";
    }

    public static class ErrorCodes
    {
        public const string StudySessionNotFound = "study_session_not_found";
        public const string SessionPlanNotFound = "session_plan_not_found";
        public const string StudyPlanNotFound = "study_plan_not_found";
        public const string StudyPlanNotActive = "study_plan_not_active";
        public const string CancelledPlanCannotBeResumed = "cancelled_plan_cannot_be_resumed";
        public const string CompletedPlanCannotBeResumed = "completed_plan_cannot_be_resumed";
        public const string CompletedPlanCannotBeCancelled = "completed_plan_cannot_be_cancelled";
        public const string CancelledPlanCannotBeCompleted = "cancelled_plan_cannot_be_completed";
        public const string SyncInvalidPayload = "sync_invalid_payload";
        public const string SyncSessionIdRequired = "sync_session_id_required";
        public const string SyncPlanIdRequired = "sync_plan_id_required";
        public const string SyncPayloadFieldRequired = "sync_payload_field_required";
        public const string SyncPayloadFieldInvalid = "sync_payload_field_invalid";
        public const string SyncOperationNotSupported = "sync_operation_not_supported";
        public const string SyncOperationIdRequired = "sync_operation_id_required";
    }
}
