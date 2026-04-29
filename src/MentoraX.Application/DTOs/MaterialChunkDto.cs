using System;
using System.Collections.Generic;
using System.Text;

namespace MentoraX.Application.DTOs;

public sealed record MaterialChunkDto(
 Guid Id,
 Guid LearningMaterialId,
 int OrderNo,
 string? Title,
 string Content,
 string? Summary,
 string? Keywords,
 int DifficultyLevel,
 int EstimatedStudyMinutes,
int CharacterCount,
    bool IsGeneratedByAI
);

