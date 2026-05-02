using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoraX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncTombstones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncTombstones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncTombstones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncTombstones_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncTombstones_UserId_DeletedAtUtc",
                table: "SyncTombstones",
                columns: new[] { "UserId", "DeletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncTombstones_UserId_EntityType_EntityId",
                table: "SyncTombstones",
                columns: new[] { "UserId", "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncTombstones");
        }
    }
}
