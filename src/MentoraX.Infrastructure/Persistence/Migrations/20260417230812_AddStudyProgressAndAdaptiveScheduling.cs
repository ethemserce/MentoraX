using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoraX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyProgressAndAdaptiveScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaterialType = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningMaterials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DailyTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyPlans_LearningMaterials_LearningMaterialId",
                        column: x => x.LearningMaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepetitionCount = table.Column<int>(type: "int", nullable: false),
                    EasinessFactor = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    SuccessStreak = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextReviewAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyProgresses_LearningMaterials_LearningMaterialId",
                        column: x => x.LearningMaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyProgresses_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyProgressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    QualityScore = table.Column<int>(type: "int", nullable: true),
                    DifficultyScore = table.Column<int>(type: "int", nullable: true),
                    ActualDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySessions_LearningMaterials_LearningMaterialId",
                        column: x => x.LearningMaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudySessions_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudySessions_StudyProgresses_StudyProgressId",
                        column: x => x.StudyProgressId,
                        principalTable: "StudyProgresses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudySessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_UserId",
                table: "LearningMaterials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_LearningMaterialId",
                table: "StudyPlans",
                column: "LearningMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_UserId",
                table: "StudyPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyProgresses_LearningMaterialId",
                table: "StudyProgresses",
                column: "LearningMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyProgresses_StudyPlanId",
                table: "StudyProgresses",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyProgresses_UserId_LearningMaterialId_StudyPlanId",
                table: "StudyProgresses",
                columns: new[] { "UserId", "LearningMaterialId", "StudyPlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_LearningMaterialId",
                table: "StudySessions",
                column: "LearningMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_StudyPlanId",
                table: "StudySessions",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_StudyProgressId",
                table: "StudySessions",
                column: "StudyProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_UserId_ScheduledAtUtc_IsCompleted",
                table: "StudySessions",
                columns: new[] { "UserId", "ScheduledAtUtc", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudySessions");

            migrationBuilder.DropTable(
                name: "StudyProgresses");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropTable(
                name: "LearningMaterials");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
