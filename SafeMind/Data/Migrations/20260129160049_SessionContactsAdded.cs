using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeMind.Data.Migrations
{
    /// <inheritdoc />
    public partial class SessionContactsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContactId",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SessionContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionContacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ContactId",
                table: "Sessions",
                column: "ContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_SessionContacts_ContactId",
                table: "Sessions",
                column: "ContactId",
                principalTable: "SessionContacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_SessionContacts_ContactId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "SessionContacts");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ContactId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Sessions");
        }
    }
}
