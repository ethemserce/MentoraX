using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoraX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialChunksAndStudyPlanItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StudyPlanItemId",
                table: "StudySessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false),
                    EstimatedStudyMinutes = table.Column<int>(type: "int", nullable: false),
                    CharacterCount = table.Column<int>(type: "int", nullable: false),
                    IsGeneratedByAI = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialChunks_LearningMaterials_LearningMaterialId",
                        column: x => x.LearningMaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudyPlanItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialChunkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    PlannedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    PlannedEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    SourceReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_MaterialChunks_MaterialChunkId",
                        column: x => x.MaterialChunkId,
                        principalTable: "MaterialChunks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_StudyPlanItemId",
                table: "StudySessions",
                column: "StudyPlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_LearningMaterialId",
                table: "MaterialChunks",
                column: "LearningMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_LearningMaterialId_OrderNo",
                table: "MaterialChunks",
                columns: new[] { "LearningMaterialId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_MaterialChunkId",
                table: "StudyPlanItems",
                column: "MaterialChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_PlannedDateUtc",
                table: "StudyPlanItems",
                column: "PlannedDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_Status",
                table: "StudyPlanItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_StudyPlanId",
                table: "StudyPlanItems",
                column: "StudyPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessions_StudyPlanItems_StudyPlanItemId",
                table: "StudySessions",
                column: "StudyPlanItemId",
                principalTable: "StudyPlanItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessions_StudyPlanItems_StudyPlanItemId",
                table: "StudySessions");

            migrationBuilder.DropTable(
                name: "StudyPlanItems");

            migrationBuilder.DropTable(
                name: "MaterialChunks");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_StudyPlanItemId",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "StudyPlanItemId",
                table: "StudySessions");
        }
    }
}
