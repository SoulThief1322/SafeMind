using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeMind.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeDoctorLicensingIntoSafeMind : Migration
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
                    LicenseNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IssuedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorLicenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenceSpecialties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenceSpecialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenceDoctorSpecialties",
                columns: table => new
                {
                    DoctorLicenseId = table.Column<int>(type: "int", nullable: false),
                    SpecialtyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenceDoctorSpecialties", x => new { x.DoctorLicenseId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_LicenceDoctorSpecialties_DoctorLicenses_DoctorLicenseId",
                        column: x => x.DoctorLicenseId,
                        principalTable: "DoctorLicenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LicenceDoctorSpecialties_LicenceSpecialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "LicenceSpecialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_LicenceDoctorSpecialties_SpecialtyId",
                table: "LicenceDoctorSpecialties",
                column: "SpecialtyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenceDoctorSpecialties");

            migrationBuilder.DropTable(
                name: "DoctorLicenses");

            migrationBuilder.DropTable(
                name: "LicenceSpecialties");
        }
    }
}
