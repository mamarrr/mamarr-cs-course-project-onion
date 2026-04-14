using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class FOOBAR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ManagementCompanies",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Customers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_management_company_slug",
                table: "ManagementCompanies",
                column: "Slug");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_customer_mcompany_slug",
                table: "Customers",
                columns: new[] { "ManagementCompanyId", "Slug" });

            migrationBuilder.CreateIndex(
                name: "ux_management_company_slug",
                table: "ManagementCompanies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customer_company_slug",
                table: "Customers",
                columns: new[] { "ManagementCompanyId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "uq_management_company_slug",
                table: "ManagementCompanies");

            migrationBuilder.DropIndex(
                name: "ux_management_company_slug",
                table: "ManagementCompanies");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_customer_mcompany_slug",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "ux_customer_company_slug",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ManagementCompanies");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Customers");
        }
    }
}
