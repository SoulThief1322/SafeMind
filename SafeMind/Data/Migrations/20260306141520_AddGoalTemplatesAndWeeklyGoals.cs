using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeMind.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalTemplatesAndWeeklyGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoalTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalTemplateId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyGoals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyGoals_GoalTemplates_GoalTemplateId",
                        column: x => x.GoalTemplateId,
                        principalTable: "GoalTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyGoals_GoalTemplateId",
                table: "WeeklyGoals",
                column: "GoalTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyGoals_UserId",
                table: "WeeklyGoals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyGoals");

            migrationBuilder.DropTable(
                name: "GoalTemplates");
        }
    }
}
