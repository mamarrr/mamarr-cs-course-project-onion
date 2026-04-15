using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class ResidentIdCodeRequiredAndUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IdCode",
                table: "Residents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "uq_resident_mcompany_idcode",
                table: "Residents",
                columns: new[] { "ManagementCompanyId", "IdCode" });

            migrationBuilder.CreateIndex(
                name: "ux_resident_company_id_code",
                table: "Residents",
                columns: new[] { "ManagementCompanyId", "IdCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "uq_resident_mcompany_idcode",
                table: "Residents");

            migrationBuilder.DropIndex(
                name: "ux_resident_company_id_code",
                table: "Residents");

            migrationBuilder.AlterColumn<string>(
                name: "IdCode",
                table: "Residents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
