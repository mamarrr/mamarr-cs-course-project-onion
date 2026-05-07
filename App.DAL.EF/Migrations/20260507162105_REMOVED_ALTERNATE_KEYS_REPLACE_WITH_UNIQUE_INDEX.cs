using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class REMOVED_ALTERNATE_KEYS_REPLACE_WITH_UNIQUE_INDEX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "uq_WORK_STATUS_CODE",
                table: "WorkStatuses");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_vtc_pair",
                table: "VendorTicketCategories");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_vendor_mcompany_registry",
                table: "Vendors");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_ticket_mcompany_ticketnr",
                table: "Tickets");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_TICKET_CATEGORY_CODE",
                table: "TicketCategories");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_resident_user_pair",
                table: "ResidentUsers");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_resident_mcompany_idcode",
                table: "Residents");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_PROPERTY_TYPE_CODE",
                table: "PropertyTypes");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_property_customer_slug",
                table: "Properties");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_mcompany_user_pair",
                table: "ManagementCompanyUsers");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_MCOMPANY_ROLE_CODE",
                table: "ManagementCompanyRoles");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_MCOMPANY_ROLE_LABEL",
                table: "ManagementCompanyRoles");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_MCOMPANY_JOIN_REQUEST_STATUS_CODE",
                table: "ManagementCompanyJoinRequestStatuses");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_management_company_slug",
                table: "ManagementCompanies");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_customer_mcompany_slug",
                table: "Customers");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_CUSTOMER_REPRESENTATIVE_ROLE_CODE",
                table: "CustomerRepresentativeRoles");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_CONTACT_TYPE_CODE",
                table: "ContactTypes");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_contact_mcompany_type_value",
                table: "Contacts");

            migrationBuilder.CreateIndex(
                name: "ux_work_status_code",
                table: "WorkStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_vtc_pair",
                table: "VendorTicketCategories",
                columns: new[] { "VendorId", "TicketCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_vendor_company_registry_code",
                table: "Vendors",
                columns: new[] { "ManagementCompanyId", "RegistryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ticket_category_code",
                table: "TicketCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_resident_user_pair",
                table: "ResidentUsers",
                columns: new[] { "ResidentId", "AppUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_property_type_code",
                table: "PropertyTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mcompany_user_pair",
                table: "ManagementCompanyUsers",
                columns: new[] { "ManagementCompanyId", "AppUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mcompany_role_code",
                table: "ManagementCompanyRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_mcompany_join_request_status_code",
                table: "ManagementCompanyJoinRequestStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_customer_representative_role_code",
                table: "CustomerRepresentativeRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_contact_type_code",
                table: "ContactTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_contact_company_type_value",
                table: "Contacts",
                columns: new[] { "ManagementCompanyId", "ContactTypeId", "ContactValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_work_status_code",
                table: "WorkStatuses");

            migrationBuilder.DropIndex(
                name: "ux_vtc_pair",
                table: "VendorTicketCategories");

            migrationBuilder.DropIndex(
                name: "ux_vendor_company_registry_code",
                table: "Vendors");

            migrationBuilder.DropIndex(
                name: "ux_ticket_category_code",
                table: "TicketCategories");

            migrationBuilder.DropIndex(
                name: "ux_resident_user_pair",
                table: "ResidentUsers");

            migrationBuilder.DropIndex(
                name: "ux_property_type_code",
                table: "PropertyTypes");

            migrationBuilder.DropIndex(
                name: "ux_mcompany_user_pair",
                table: "ManagementCompanyUsers");

            migrationBuilder.DropIndex(
                name: "ux_mcompany_role_code",
                table: "ManagementCompanyRoles");

            migrationBuilder.DropIndex(
                name: "ux_mcompany_join_request_status_code",
                table: "ManagementCompanyJoinRequestStatuses");

            migrationBuilder.DropIndex(
                name: "ux_customer_representative_role_code",
                table: "CustomerRepresentativeRoles");

            migrationBuilder.DropIndex(
                name: "ux_contact_type_code",
                table: "ContactTypes");

            migrationBuilder.DropIndex(
                name: "ux_contact_company_type_value",
                table: "Contacts");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_WORK_STATUS_CODE",
                table: "WorkStatuses",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_vtc_pair",
                table: "VendorTicketCategories",
                columns: new[] { "VendorId", "TicketCategoryId" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_vendor_mcompany_registry",
                table: "Vendors",
                columns: new[] { "ManagementCompanyId", "RegistryCode" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_ticket_mcompany_ticketnr",
                table: "Tickets",
                columns: new[] { "ManagementCompanyId", "TicketNr" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_TICKET_CATEGORY_CODE",
                table: "TicketCategories",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_resident_user_pair",
                table: "ResidentUsers",
                columns: new[] { "ResidentId", "AppUserId" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_resident_mcompany_idcode",
                table: "Residents",
                columns: new[] { "ManagementCompanyId", "IdCode" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_PROPERTY_TYPE_CODE",
                table: "PropertyTypes",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_property_customer_slug",
                table: "Properties",
                columns: new[] { "CustomerId", "Slug" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_mcompany_user_pair",
                table: "ManagementCompanyUsers",
                columns: new[] { "ManagementCompanyId", "AppUserId" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_MCOMPANY_ROLE_CODE",
                table: "ManagementCompanyRoles",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_MCOMPANY_ROLE_LABEL",
                table: "ManagementCompanyRoles",
                column: "Label");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_MCOMPANY_JOIN_REQUEST_STATUS_CODE",
                table: "ManagementCompanyJoinRequestStatuses",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_management_company_slug",
                table: "ManagementCompanies",
                column: "Slug");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_customer_mcompany_slug",
                table: "Customers",
                columns: new[] { "ManagementCompanyId", "Slug" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_CUSTOMER_REPRESENTATIVE_ROLE_CODE",
                table: "CustomerRepresentativeRoles",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_CONTACT_TYPE_CODE",
                table: "ContactTypes",
                column: "Code");

            migrationBuilder.AddUniqueConstraint(
                name: "uq_contact_mcompany_type_value",
                table: "Contacts",
                columns: new[] { "ManagementCompanyId", "ContactTypeId", "ContactValue" });
        }
    }
}
