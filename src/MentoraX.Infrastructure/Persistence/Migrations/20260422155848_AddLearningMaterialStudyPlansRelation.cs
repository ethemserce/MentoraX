using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoraX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningMaterialStudyPlansRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LearningMaterialId1",
                table: "StudyPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_LearningMaterialId1",
                table: "StudyPlans",
                column: "LearningMaterialId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyPlans_LearningMaterials_LearningMaterialId1",
                table: "StudyPlans",
                column: "LearningMaterialId1",
                principalTable: "LearningMaterials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudyPlans_LearningMaterials_LearningMaterialId1",
                table: "StudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_StudyPlans_LearningMaterialId1",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "LearningMaterialId1",
                table: "StudyPlans");
        }
    }
}
