using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class ProfileEditableBusinessKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "uq_MANAGMENT_COMPANY_REGISTRY_CODE",
                table: "ManagementCompanies");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_customer_mcompany_registry",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "ux_management_company_registry_code",
                table: "ManagementCompanies",
                column: "RegistryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customer_company_registry_code",
                table: "Customers",
                columns: new[] { "ManagementCompanyId", "RegistryCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_management_company_registry_code",
                table: "ManagementCompanies");

            migrationBuilder.DropIndex(
                name: "ux_customer_company_registry_code",
                table: "Customers");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_MANAGMENT_COMPANY_REGISTRY_CODE",
                table: "ManagementCompanies",
                column: "RegistryCode");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_customer_mcompany_registry",
                table: "Customers",
                columns: new[] { "ManagementCompanyId", "RegistryCode" });
        }
    }
}
