using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeMind.Data.Migrations.DoctorLicensing
{
    /// <inheritdoc />
    public partial class InitDoctorLicensing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorLicenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicenseNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IssuedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorLicenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorLicenses_LicenseNumber",
                table: "DoctorLicenses",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorLicenses_NationalId",
                table: "DoctorLicenses",
                column: "NationalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorLicenses");
        }
    }
}
