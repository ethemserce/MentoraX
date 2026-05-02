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
        public const string MaterialCreated = "MaterialCreated";
        public const string MaterialChunkCreated = "MaterialChunkCreated";
        public const string MaterialChunkUpdated = "MaterialChunkUpdated";
        public const string MaterialChunkDeleted = "MaterialChunkDeleted";
        public const string MaterialChunksReordered = "MaterialChunksReordered";
    }

    public static class EntityTypes
    {
        public const string StudySession = "StudySession";
        public const string StudyPlan = "StudyPlan";
        public const string Material = "Material";
        public const string MaterialChunk = "MaterialChunk";
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
        public const string LearningMaterialNotFound = "learning_material_not_found";
        public const string MaterialChunkNotFound = "material_chunk_not_found";
        public const string MaterialIdConflict = "material_id_conflict";
        public const string MaterialChunkIdConflict = "material_chunk_id_conflict";
        public const string ChunkContentRequired = "chunk_content_required";
        public const string InvalidDifficultyLevel = "invalid_difficulty_level";
        public const string InvalidEstimatedStudyMinutes = "invalid_estimated_study_minutes";
        public const string ChunkIsUsedInStudyPlan = "chunk_is_used_in_study_plan";
        public const string ChunkOrderRequired = "chunk_order_required";
        public const string InvalidChunkOrderCount = "invalid_chunk_order_count";
        public const string InvalidChunkOrderIds = "invalid_chunk_order_ids";
        public const string SyncInvalidPayload = "sync_invalid_payload";
        public const string SyncSessionIdRequired = "sync_session_id_required";
        public const string SyncPlanIdRequired = "sync_plan_id_required";
        public const string SyncMaterialIdRequired = "sync_material_id_required";
        public const string SyncChunkIdRequired = "sync_chunk_id_required";
        public const string SyncPayloadFieldRequired = "sync_payload_field_required";
        public const string SyncPayloadFieldInvalid = "sync_payload_field_invalid";
        public const string SyncOperationNotSupported = "sync_operation_not_supported";
        public const string SyncOperationIdRequired = "sync_operation_id_required";
    }
}
