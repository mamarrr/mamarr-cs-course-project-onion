using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class ManagementCompanyJoinRequestsOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagementCompanyJoinRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagementCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedManagementCompanyRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByAppUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementCompanyJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "fk_mcompany_join_request_appuser",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mcompany_join_request_mcompany",
                        column: x => x.ManagementCompanyId,
                        principalTable: "ManagementCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mcompany_join_request_requested_role",
                        column: x => x.RequestedManagementCompanyRoleId,
                        principalTable: "ManagementCompanyRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mcompany_join_request_resolver_appuser",
                        column: x => x.ResolvedByAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_appuser_id_fk",
                table: "ManagementCompanyJoinRequests",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_company_status_created_at",
                table: "ManagementCompanyJoinRequests",
                columns: new[] { "ManagementCompanyId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_mcompany_id_fk",
                table: "ManagementCompanyJoinRequests",
                column: "ManagementCompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_requested_role_id_fk",
                table: "ManagementCompanyJoinRequests",
                column: "RequestedManagementCompanyRoleId");

            migrationBuilder.CreateIndex(
                name: "ix_mcompany_join_request_resolved_by_appuser_id_fk",
                table: "ManagementCompanyJoinRequests",
                column: "ResolvedByAppUserId");

            migrationBuilder.CreateIndex(
                name: "ux_mcompany_join_request_pending_user_company",
                table: "ManagementCompanyJoinRequests",
                columns: new[] { "AppUserId", "ManagementCompanyId" },
                unique: true,
                filter: "\"Status\" = 'PENDING'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagementCompanyJoinRequests");
        }
    }
}
