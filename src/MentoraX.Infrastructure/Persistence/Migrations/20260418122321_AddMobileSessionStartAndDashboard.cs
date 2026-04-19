using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentoraX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileSessionStartAndDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "StudySessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "StudySessions");
        }
    }
}
