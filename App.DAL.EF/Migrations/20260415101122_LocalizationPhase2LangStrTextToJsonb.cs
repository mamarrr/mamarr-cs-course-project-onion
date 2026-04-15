using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class LocalizationPhase2LangStrTextToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "WorkLogs" ALTER COLUMN "Description" TYPE jsonb
                USING CASE WHEN "Description" IS NULL THEN NULL ELSE jsonb_build_object('en', "Description") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "VendorTicketCategories" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Vendors" ALTER COLUMN "Notes" TYPE jsonb
                USING jsonb_build_object('en', "Notes");
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "VendorContacts" ALTER COLUMN "RoleTitle" TYPE jsonb
                USING CASE WHEN "RoleTitle" IS NULL THEN NULL ELSE jsonb_build_object('en', "RoleTitle") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Units" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Tickets" ALTER COLUMN "Title" TYPE jsonb
                USING jsonb_build_object('en', "Title");
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Tickets" ALTER COLUMN "Description" TYPE jsonb
                USING jsonb_build_object('en', "Description");
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ScheduledWorks" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Properties" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Properties" ALTER COLUMN "Label" TYPE jsonb
                USING jsonb_build_object('en', "Label");
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ManagementCompanyUsers" ALTER COLUMN "JobTitle" TYPE jsonb
                USING jsonb_build_object('en', "JobTitle");
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ManagementCompanyJoinRequests" ALTER COLUMN "Message" TYPE jsonb
                USING CASE WHEN "Message" IS NULL THEN NULL ELSE jsonb_build_object('en', "Message") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Leases" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Customers" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CustomerRepresentatives" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Contacts" ALTER COLUMN "Notes" TYPE jsonb
                USING CASE WHEN "Notes" IS NULL THEN NULL ELSE jsonb_build_object('en', "Notes") END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "WorkLogs" ALTER COLUMN "Description" TYPE text
                USING CASE
                    WHEN "Description" IS NULL THEN NULL
                    WHEN jsonb_typeof("Description") = 'object' THEN "Description"->>'en'
                    WHEN jsonb_typeof("Description") = 'string' THEN "Description" #>> '{}'
                    ELSE "Description"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "VendorTicketCategories" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Vendors" ALTER COLUMN "Notes" TYPE text
                USING COALESCE(
                    CASE
                        WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                        WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                        ELSE "Notes"::text
                    END,
                    ''
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "VendorContacts" ALTER COLUMN "RoleTitle" TYPE character varying(200)
                USING CASE
                    WHEN "RoleTitle" IS NULL THEN NULL
                    WHEN jsonb_typeof("RoleTitle") = 'object' THEN "RoleTitle"->>'en'
                    WHEN jsonb_typeof("RoleTitle") = 'string' THEN "RoleTitle" #>> '{}'
                    ELSE "RoleTitle"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Units" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Tickets" ALTER COLUMN "Title" TYPE character varying(255)
                USING COALESCE(
                    CASE
                        WHEN jsonb_typeof("Title") = 'object' THEN "Title"->>'en'
                        WHEN jsonb_typeof("Title") = 'string' THEN "Title" #>> '{}'
                        ELSE "Title"::text
                    END,
                    ''
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Tickets" ALTER COLUMN "Description" TYPE text
                USING COALESCE(
                    CASE
                        WHEN jsonb_typeof("Description") = 'object' THEN "Description"->>'en'
                        WHEN jsonb_typeof("Description") = 'string' THEN "Description" #>> '{}'
                        ELSE "Description"::text
                    END,
                    ''
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ScheduledWorks" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Properties" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Properties" ALTER COLUMN "Label" TYPE character varying(200)
                USING COALESCE(
                    CASE
                        WHEN jsonb_typeof("Label") = 'object' THEN "Label"->>'en'
                        WHEN jsonb_typeof("Label") = 'string' THEN "Label" #>> '{}'
                        ELSE "Label"::text
                    END,
                    ''
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ManagementCompanyUsers" ALTER COLUMN "JobTitle" TYPE character varying(255)
                USING COALESCE(
                    CASE
                        WHEN jsonb_typeof("JobTitle") = 'object' THEN "JobTitle"->>'en'
                        WHEN jsonb_typeof("JobTitle") = 'string' THEN "JobTitle" #>> '{}'
                        ELSE "JobTitle"::text
                    END,
                    ''
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "ManagementCompanyJoinRequests" ALTER COLUMN "Message" TYPE character varying(2000)
                USING CASE
                    WHEN "Message" IS NULL THEN NULL
                    WHEN jsonb_typeof("Message") = 'object' THEN "Message"->>'en'
                    WHEN jsonb_typeof("Message") = 'string' THEN "Message" #>> '{}'
                    ELSE "Message"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Leases" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Customers" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CustomerRepresentatives" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Contacts" ALTER COLUMN "Notes" TYPE text
                USING CASE
                    WHEN "Notes" IS NULL THEN NULL
                    WHEN jsonb_typeof("Notes") = 'object' THEN "Notes"->>'en'
                    WHEN jsonb_typeof("Notes") = 'string' THEN "Notes" #>> '{}'
                    ELSE "Notes"::text
                END;
                """);
        }
    }
}
