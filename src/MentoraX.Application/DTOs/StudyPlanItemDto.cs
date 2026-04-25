using System;
using System.Collections.Generic;
using System.Text;

namespace MentoraX.Application.DTOs;

public sealed record StudyPlanItemDto(
Guid Id,
Guid StudyPlanId,
Guid? MaterialChunkId,
string Title,
string? Description,
string ItemType,
int OrderNo,
DateTime PlannedDateUtc,
TimeSpan? PlannedStartTime,
TimeSpan? PlannedEndTime,
int DurationMinutes,
string Status,
MaterialChunkDto? MaterialChunk,
IReadOnlyCollection<StudySessionDto> Sessions
);

