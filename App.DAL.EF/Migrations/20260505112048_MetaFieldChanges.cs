using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class MetaFieldChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vendor_active_by_company",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "ix_resident_active_by_company",
                table: "Residents");

            migrationBuilder.DropIndex(
                name: "ix_lease_resident_unit_active_start_date",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "ix_customer_active_by_company",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "VendorTicketCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ResidentUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ManagementCompanyUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ManagementCompanies");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CustomerRepresentatives");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorTicketCategories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorContacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ResidentContacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Leases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "ix_vendor_by_company",
                table: "Vendors",
                column: "ManagementCompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_resident_by_company",
                table: "Residents",
                column: "ManagementCompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_lease_resident_unit_start_date",
                table: "Leases",
                columns: new[] { "ResidentId", "UnitId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_by_company",
                table: "Customers",
                column: "ManagementCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vendor_by_company",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "ix_resident_by_company",
                table: "Residents");

            migrationBuilder.DropIndex(
                name: "ix_lease_resident_unit_start_date",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "ix_customer_by_company",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VendorTicketCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VendorContacts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ResidentContacts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Leases");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "VendorTicketCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Units",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ResidentUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ManagementCompanyUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ManagementCompanies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Leases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CustomerRepresentatives",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_active_by_company",
                table: "Vendors",
                column: "ManagementCompanyId",
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_resident_active_by_company",
                table: "Residents",
                column: "ManagementCompanyId",
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_lease_resident_unit_active_start_date",
                table: "Leases",
                columns: new[] { "ResidentId", "UnitId", "IsActive", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_active_by_company",
                table: "Customers",
                column: "ManagementCompanyId",
                filter: "\"IsActive\" = TRUE");
        }
    }
}
