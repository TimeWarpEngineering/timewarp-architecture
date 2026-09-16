using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropSiteSettingsEntraTrustedTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntraTrustedTenants",
                schema: "identity",
                table: "site_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntraTrustedTenants",
                schema: "identity",
                table: "site_settings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }
    }
}
