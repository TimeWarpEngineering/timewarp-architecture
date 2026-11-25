using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261125130740_AddFeedbackFilingContext : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AppVersion",
            schema: "feedback",
            table: "feedback_items",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PagePath",
            schema: "feedback",
            table: "feedback_items",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AppVersion",
            schema: "feedback",
            table: "feedback_items");

        migrationBuilder.DropColumn(
            name: "PagePath",
            schema: "feedback",
            table: "feedback_items");
    }
}
