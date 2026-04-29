using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class JoinRequestStatusLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mcompany_join_request_company_status_created_at",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.DropIndex(
                name: "ux_mcompany_join_request_pending_user_company",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "ManagementCompanyJoinRequestStatusId",
                table: "ManagementCompanyJoinRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateTable(
                name: "ManagementCompanyJoinRequestStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementCompanyJoinRequestStatuses", x => x.Id);
                    table.UniqueConstraint("uq_MCOMPANY_JOIN_REQUEST_STATUS_CODE", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "ManagementCompanyJoinRequestStatuses",
                columns: new[] { "Id", "Code", "Label" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "PENDING", "{\"en\":\"Pending\",\"et\":\"Ootel\"}" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "APPROVED", "{\"en\":\"Approved\",\"et\":\"Kinnitatud\"}" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "REJECTED", "{\"en\":\"Rejected\",\"et\":\"Tagasi lükatud\"}" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_company_status_created_at",
                table: "ManagementCompanyJoinRequests",
                columns: new[] { "ManagementCompanyId", "ManagementCompanyJoinRequestStatusId", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_status_id_fk",
                table: "ManagementCompanyJoinRequests",
                column: "ManagementCompanyJoinRequestStatusId");

            migrationBuilder.CreateIndex(
                name: "ux_mcompany_join_request_pending_user_company",
                table: "ManagementCompanyJoinRequests",
                columns: new[] { "AppUserId", "ManagementCompanyId" },
                unique: true,
                filter: "\"ManagementCompanyJoinRequestStatusId\" = '11111111-1111-1111-1111-111111111111'");

            migrationBuilder.AddForeignKey(
                name: "fk_mcompany_join_request_status",
                table: "ManagementCompanyJoinRequests",
                column: "ManagementCompanyJoinRequestStatusId",
                principalTable: "ManagementCompanyJoinRequestStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mcompany_join_request_status",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.DropTable(
                name: "ManagementCompanyJoinRequestStatuses");

            migrationBuilder.DropIndex(
                name: "ix_mcompany_join_request_company_status_created_at",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.DropIndex(
                name: "ix_mcompany_join_request_status_id_fk",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.DropIndex(
                name: "ux_mcompany_join_request_pending_user_company",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.DropColumn(
                name: "ManagementCompanyJoinRequestStatusId",
                table: "ManagementCompanyJoinRequests");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ManagementCompanyJoinRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_company_status_created_at",
                table: "ManagementCompanyJoinRequests",
                columns: new[] { "ManagementCompanyId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_mcompany_join_request_pending_user_company",
                table: "ManagementCompanyJoinRequests",
                columns: new[] { "AppUserId", "ManagementCompanyId" },
                unique: true,
                filter: "\"Status\" = 'PENDING'");
        }
    }
}
