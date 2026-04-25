using MentoraX.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MentoraX.Domain.Entities
{
    public class StudyPlanItem : BaseEntity
    {
        private StudyPlanItem()
        {
        }

        public StudyPlanItem(
            Guid studyPlanId,
            Guid? materialChunkId,
            string title,
            string? description,
            StudyItemType itemType,
            int orderNo,
            DateTime plannedDateUtc,
            TimeSpan? plannedStartTime,
            TimeSpan? plannedEndTime,
            int durationMinutes,
            int priority,
            bool isMandatory,
            string? sourceReason)
        {
            Id = Guid.NewGuid();
            StudyPlanId = studyPlanId;
            MaterialChunkId = materialChunkId;
            Title = title;
            Description = description;
            ItemType = itemType;
            OrderNo = orderNo;
            PlannedDateUtc = plannedDateUtc;
            PlannedStartTime = plannedStartTime;
            PlannedEndTime = plannedEndTime;
            DurationMinutes = durationMinutes;
            Priority = priority;
            IsMandatory = isMandatory;
            SourceReason = sourceReason;
            Status = StudyPlanItemStatus.Pending;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        public Guid StudyPlanId { get; private set; }

        public Guid? MaterialChunkId { get; private set; }

        public string Title { get; private set; } = default!;

        public string? Description { get; private set; }

        public StudyItemType ItemType { get; private set; }

        public int OrderNo { get; private set; }

        public DateTime PlannedDateUtc { get; private set; }

        public TimeSpan? PlannedStartTime { get; private set; }

        public TimeSpan? PlannedEndTime { get; private set; }

        public int DurationMinutes { get; private set; }

        public int Priority { get; private set; }

        public StudyPlanItemStatus Status { get; private set; }

        public bool IsMandatory { get; private set; }

        public string? SourceReason { get; private set; }

        public StudyPlan StudyPlan { get; private set; } = default!;

        public MaterialChunk? MaterialChunk { get; private set; }

        public ICollection<StudySession> StudySessions { get; private set; } = new List<StudySession>();

        public void MarkInProgress()
        {
            if (Status == StudyPlanItemStatus.Completed)
                return;

            Status = StudyPlanItemStatus.InProgress;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void MarkCompleted()
        {
            Status = StudyPlanItemStatus.Completed;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void Skip()
        {
            Status = StudyPlanItemStatus.Skipped;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = StudyPlanItemStatus.Cancelled;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
