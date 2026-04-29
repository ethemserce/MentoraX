using System;
using System.Collections.Generic;
using System.Text;

namespace MentoraX.Domain.Entities
{
    public class MaterialChunk : BaseEntity
    {
        private MaterialChunk()
        {
        }

        public MaterialChunk(
            Guid learningMaterialId,
            int orderNo,
            string content,
            string? title,
            string? summary,
            string? keywords,
            int difficultyLevel,
            int estimatedStudyMinutes,
            bool isGeneratedByAI = false)
        {
            Id = Guid.NewGuid();
            LearningMaterialId = learningMaterialId;
            OrderNo = orderNo;
            Content = content;
            Title = title;
            Summary = summary;
            Keywords = keywords;
            DifficultyLevel = difficultyLevel;
            EstimatedStudyMinutes = estimatedStudyMinutes;
            CharacterCount = content.Length;
            IsGeneratedByAI = isGeneratedByAI;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }

        public Guid LearningMaterialId { get; private set; }

        public int OrderNo { get; private set; }

        public string? Title { get; private set; }

        public string Content { get; private set; } = default!;

        public string? Summary { get; private set; }

        public string? Keywords { get; private set; }

        public int DifficultyLevel { get; private set; }

        public int EstimatedStudyMinutes { get; private set; }

        public int CharacterCount { get; private set; }

        public bool IsGeneratedByAI { get; private set; }

        public LearningMaterial LearningMaterial { get; private set; } = default!;

        public ICollection<StudyPlanItem> StudyPlanItems { get; private set; } = new List<StudyPlanItem>();

        public void Update(
            string content,
            string? title,
            string? summary,
            string? keywords,
            int difficultyLevel,
            int estimatedStudyMinutes)
        {
            Content = content;
            Title = title;
            Summary = summary;
            Keywords = keywords;
            DifficultyLevel = difficultyLevel;
            EstimatedStudyMinutes = estimatedStudyMinutes;
            CharacterCount = content.Length;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void ChangeOrder(int orderNo)
        {
            if (orderNo <= 0)
                throw new ArgumentOutOfRangeException(nameof(orderNo), "Order number must be greater than zero.");

            OrderNo = orderNo;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void ChangeOrderTemporary(int orderNo)
        {
            OrderNo = orderNo;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
