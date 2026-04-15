using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class PropertyAndUnitSlugConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "uq_unit_property_slug",
                table: "Units",
                columns: new[] { "PropertyId", "Slug" });

            migrationBuilder.AddUniqueConstraint(
                name: "uq_property_customer_slug",
                table: "Properties",
                columns: new[] { "CustomerId", "Slug" });

            migrationBuilder.CreateIndex(
                name: "ux_unit_property_slug",
                table: "Units",
                columns: new[] { "PropertyId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_property_customer_slug",
                table: "Properties",
                columns: new[] { "CustomerId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "uq_unit_property_slug",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "ux_unit_property_slug",
                table: "Units");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_property_customer_slug",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "ux_property_customer_slug",
                table: "Properties");
        }
    }
}
