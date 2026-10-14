using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddSiteSettingsAssistantName : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The assistant's display name (task 361); existing rows get the template default, Akili.
        migrationBuilder.AddColumn<string>(
            name: "AssistantName",
            schema: "identity",
            table: "site_settings",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Akili");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AssistantName",
            schema: "identity",
            table: "site_settings");
    }
}
