using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRental.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeTheDriverLicenceOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DriverLicenseNumber",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "DriverLicenseNumber",
                table: "AspNetUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DriverLicenseNumber",
                table: "AspNetUsers",
                column: "DriverLicenseNumber",
                unique: true,
                filter: "\"DriverLicenseNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DriverLicenseNumber",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "DriverLicenseNumber",
                table: "AspNetUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DriverLicenseNumber",
                table: "AspNetUsers",
                column: "DriverLicenseNumber",
                unique: true);
        }
    }
}
