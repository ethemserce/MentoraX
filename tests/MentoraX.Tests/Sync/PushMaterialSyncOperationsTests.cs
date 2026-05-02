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

public sealed class PushMaterialSyncOperationsTests
{
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Handle_AppliesMaterialCreatedOperation_WithClientMaterialAndDefaultChunkIds()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var materialId = Guid.NewGuid();
        var defaultChunkId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "create-material-1",
                    operationType: SyncContract.OperationTypes.MaterialCreated,
                    entityType: SyncContract.EntityTypes.Material,
                    entityId: materialId,
                    occurredAtUtc: occurredAtUtc,
                    payload: new
                    {
                        materialId,
                        defaultChunkId,
                        title = "Offline Material",
                        materialType = "Text",
                        content = "Offline material content",
                        estimatedDurationMinutes = 40,
                        description = "Offline description",
                        tags = "offline,sync"
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var material = await dbContext.LearningMaterials.SingleAsync(x => x.Id == materialId);
        var chunk = await dbContext.MaterialChunks.SingleAsync(x => x.Id == defaultChunkId);
        var recordedOperation = await dbContext.SyncOperations.SingleAsync();

        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);
        Assert.Equal(UserId, material.UserId);
        Assert.Equal("Offline Material", material.Title);
        Assert.Equal(MaterialType.Text, material.MaterialType);
        Assert.Equal("Offline material content", material.Content);
        Assert.Equal(40, material.EstimatedDurationMinutes);
        Assert.Equal(defaultChunkId, chunk.Id);
        Assert.Equal(materialId, chunk.LearningMaterialId);
        Assert.Equal(1, chunk.OrderNo);
        Assert.Equal("Offline Material", chunk.Title);
        Assert.Equal("Offline material content", chunk.Content);
        Assert.Equal(SyncContract.ResultStatuses.Applied, recordedOperation.Status);
    }

    [Fact]
    public async Task Handle_AppliesMaterialChunkCreatedOperation_WithClientChunkId()
    {
        await using var dbContext = CreateDbContext();
        var materialId = await SeedMaterialAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var chunkId = Guid.NewGuid();

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "create-chunk-1",
                    operationType: SyncContract.OperationTypes.MaterialChunkCreated,
                    entityType: SyncContract.EntityTypes.MaterialChunk,
                    entityId: chunkId,
                    occurredAtUtc: DateTime.UtcNow,
                    payload: new
                    {
                        materialId,
                        chunkId,
                        title = "Offline Chunk",
                        content = "Offline chunk content",
                        summary = "Chunk summary",
                        keywords = "chunk,offline",
                        difficultyLevel = 3,
                        estimatedStudyMinutes = 22,
                        orderNo = 1
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var chunk = await dbContext.MaterialChunks.SingleAsync(x => x.Id == chunkId);

        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);
        Assert.Equal(materialId, chunk.LearningMaterialId);
        Assert.Equal("Offline Chunk", chunk.Title);
        Assert.Equal("Offline chunk content", chunk.Content);
        Assert.Equal("Chunk summary", chunk.Summary);
        Assert.Equal("chunk,offline", chunk.Keywords);
        Assert.Equal(3, chunk.DifficultyLevel);
        Assert.Equal(22, chunk.EstimatedStudyMinutes);
        Assert.Equal(1, chunk.OrderNo);
    }

    [Fact]
    public async Task Handle_AppliesMaterialChunkUpdatedOperation()
    {
        await using var dbContext = CreateDbContext();
        var materialId = await SeedMaterialAsync(dbContext);
        var chunkId = await SeedChunkAsync(dbContext, materialId, orderNo: 1);
        var handler = CreateHandler(dbContext);
        var occurredAtUtc = DateTime.UtcNow;

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "update-chunk-1",
                    operationType: SyncContract.OperationTypes.MaterialChunkUpdated,
                    entityType: SyncContract.EntityTypes.MaterialChunk,
                    entityId: chunkId,
                    occurredAtUtc: occurredAtUtc,
                    payload: new
                    {
                        materialId,
                        chunkId,
                        title = "Updated Chunk",
                        content = "Updated chunk content",
                        summary = "Updated summary",
                        keywords = "updated",
                        difficultyLevel = 4,
                        estimatedStudyMinutes = 30
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var chunk = await dbContext.MaterialChunks.SingleAsync(x => x.Id == chunkId);

        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);
        Assert.Equal("Updated Chunk", chunk.Title);
        Assert.Equal("Updated chunk content", chunk.Content);
        Assert.Equal("Updated summary", chunk.Summary);
        Assert.Equal("updated", chunk.Keywords);
        Assert.Equal(4, chunk.DifficultyLevel);
        Assert.Equal(30, chunk.EstimatedStudyMinutes);
        Assert.Equal("Updated chunk content".Length, chunk.CharacterCount);
        Assert.Equal(occurredAtUtc, chunk.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_AppliesMaterialChunkDeletedOperation_WithTombstoneAndReorderedRemainingChunks()
    {
        await using var dbContext = CreateDbContext();
        var materialId = await SeedMaterialAsync(dbContext);
        var firstChunkId = await SeedChunkAsync(dbContext, materialId, orderNo: 1);
        var secondChunkId = await SeedChunkAsync(dbContext, materialId, orderNo: 2);
        var handler = CreateHandler(dbContext);
        var deletedAtUtc = DateTime.UtcNow;

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "delete-chunk-1",
                    operationType: SyncContract.OperationTypes.MaterialChunkDeleted,
                    entityType: SyncContract.EntityTypes.MaterialChunk,
                    entityId: firstChunkId,
                    occurredAtUtc: deletedAtUtc,
                    payload: new
                    {
                        materialId,
                        chunkId = firstChunkId
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var chunks = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == materialId)
            .ToListAsync();
        var tombstone = await dbContext.SyncTombstones.SingleAsync();
        var remainingChunk = Assert.Single(chunks);

        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);
        Assert.Equal(secondChunkId, remainingChunk.Id);
        Assert.Equal(1, remainingChunk.OrderNo);
        Assert.Equal(SyncContract.EntityTypes.MaterialChunk, tombstone.EntityType);
        Assert.Equal(firstChunkId, tombstone.EntityId);
        Assert.Equal(deletedAtUtc, tombstone.DeletedAtUtc);
    }

    [Fact]
    public async Task Handle_AppliesMaterialChunksReorderedOperation()
    {
        await using var dbContext = CreateDbContext();
        var materialId = await SeedMaterialAsync(dbContext);
        var firstChunkId = await SeedChunkAsync(dbContext, materialId, orderNo: 1);
        var secondChunkId = await SeedChunkAsync(dbContext, materialId, orderNo: 2);
        var thirdChunkId = await SeedChunkAsync(dbContext, materialId, orderNo: 3);
        var handler = CreateHandler(dbContext);

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "reorder-chunks-1",
                    operationType: SyncContract.OperationTypes.MaterialChunksReordered,
                    entityType: SyncContract.EntityTypes.Material,
                    entityId: materialId,
                    occurredAtUtc: DateTime.UtcNow,
                    payload: new
                    {
                        materialId,
                        chunkIds = new[] { thirdChunkId, firstChunkId, secondChunkId }
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var orderedChunks = await dbContext.MaterialChunks
            .Where(x => x.LearningMaterialId == materialId)
            .OrderBy(x => x.OrderNo)
            .ToListAsync();

        Assert.Equal(SyncContract.ResultStatuses.Applied, result.Status);
        Assert.Equal(
            new[] { thirdChunkId, firstChunkId, secondChunkId },
            orderedChunks.Select(x => x.Id).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, orderedChunks.Select(x => x.OrderNo).ToArray());
    }

    [Fact]
    public async Task Handle_RecordsConflict_WhenMaterialChunkCreateReferencesMissingMaterial()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext);
        var handler = CreateHandler(dbContext);
        var chunkId = Guid.NewGuid();

        var response = await handler.Handle(
            new PushSyncOperationsCommand([
                Operation(
                    operationId: "create-missing-material-chunk",
                    operationType: SyncContract.OperationTypes.MaterialChunkCreated,
                    entityType: SyncContract.EntityTypes.MaterialChunk,
                    entityId: chunkId,
                    occurredAtUtc: DateTime.UtcNow,
                    payload: new
                    {
                        materialId = Guid.NewGuid(),
                        chunkId,
                        title = "Missing material chunk",
                        content = "Chunk content",
                        difficultyLevel = 2,
                        estimatedStudyMinutes = 15
                    })
            ]),
            CancellationToken.None);

        var result = Assert.Single(response.Results);
        var recordedOperation = await dbContext.SyncOperations.SingleAsync();

        Assert.Equal(SyncContract.ResultStatuses.Conflict, result.Status);
        Assert.StartsWith(
            $"{SyncContract.ErrorCodes.LearningMaterialNotFound}:",
            result.Error);
        Assert.Equal(SyncContract.ResultStatuses.Conflict, recordedOperation.Status);
    }

    private static MentoraXDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MentoraXDbContext>()
            .UseInMemoryDatabase($"mentorax-material-sync-tests-{Guid.NewGuid()}")
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

    private static async Task SeedUserAsync(MentoraXDbContext dbContext)
    {
        dbContext.Users.Add(new User(
            "Material Sync User",
            "material-sync@mentorax.local",
            "password-hash")
        {
            Id = UserId
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> SeedMaterialAsync(MentoraXDbContext dbContext)
    {
        await SeedUserAsync(dbContext);

        var materialId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var material = new LearningMaterial(
            UserId,
            "Seed Material",
            MaterialType.Text,
            "Seed material content",
            30)
        {
            Id = materialId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.LearningMaterials.Add(material);
        await dbContext.SaveChangesAsync();

        return materialId;
    }

    private static async Task<Guid> SeedChunkAsync(
        MentoraXDbContext dbContext,
        Guid materialId,
        int orderNo)
    {
        var chunkId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var chunk = new MaterialChunk(
            learningMaterialId: materialId,
            orderNo: orderNo,
            content: $"Seed chunk {orderNo} content",
            title: $"Seed Chunk {orderNo}",
            summary: null,
            keywords: null,
            difficultyLevel: 2,
            estimatedStudyMinutes: 20,
            isGeneratedByAI: false,
            id: chunkId)
        {
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.MaterialChunks.Add(chunk);
        await dbContext.SaveChangesAsync();

        return chunkId;
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

    private sealed class FakeCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid GetRequiredUserId() => userId;

        public Guid? GetUserId() => userId;

        public string? GetEmail() => "material-sync@mentorax.local";
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
                RepetitionCount: repetitionCount + 1,
                IntervalDays: previousIntervalDays + 1,
                EasinessFactor: easinessFactor,
                NextReviewAtUtc: reviewedAtUtc.AddDays(1),
                SuccessStreakDelta: 1,
                IsFailure: false);
        }
    }
}
