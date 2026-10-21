using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSiteSettingsEntraTenantAcceptance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Task 306: existing rows stay Restricted, uninitialized, and with an empty list.
        // The seeder copies a GUID Authentication:Entra:TenantId into that list once.
        migrationBuilder.AddColumn<string>(
            name: "EntraAcceptedTenantIds",
            schema: "identity",
            table: "site_settings",
            type: "jsonb",
            nullable: false,
            defaultValueSql: "'[]'::jsonb");

        migrationBuilder.AddColumn<bool>(
            name: "EntraTenantAcceptanceInitialized",
            schema: "identity",
            table: "site_settings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "EntraTenantAcceptanceMode",
            schema: "identity",
            table: "site_settings",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EntraAcceptedTenantIds",
            schema: "identity",
            table: "site_settings");

        migrationBuilder.DropColumn(
            name: "EntraTenantAcceptanceInitialized",
            schema: "identity",
            table: "site_settings");

        migrationBuilder.DropColumn(
            name: "EntraTenantAcceptanceMode",
            schema: "identity",
            table: "site_settings");
    }
}
