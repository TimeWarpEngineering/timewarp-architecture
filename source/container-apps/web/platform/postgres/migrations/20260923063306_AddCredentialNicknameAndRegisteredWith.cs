using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialNicknameAndRegisteredWith : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                schema: "identity",
                table: "credentials",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // Task 248-001 review M1: before this migration a caller-supplied name was written into Label.
            // Agent keys have no provider, so every existing agent-key Label is by definition a user name —
            // move it to Nickname (capped at the new column length) and clear Label so it does not render
            // as the provider. Passkey rows cannot be told apart (AAGUID name vs user name) and are left as-is.
            migrationBuilder.Sql(
                "UPDATE identity.credentials SET \"Nickname\" = LEFT(\"Label\", 64), \"Label\" = NULL "
                + "WHERE \"Type\" = 2 AND \"Label\" IS NOT NULL;");

            migrationBuilder.AddColumn<int>(
                name: "RegisteredAttachment",
                schema: "identity",
                table: "credentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredBrowser",
                schema: "identity",
                table: "credentials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredOs",
                schema: "identity",
                table: "credentials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Inverse of the Up data step: restore agent-key nicknames into Label before the column goes.
            migrationBuilder.Sql(
                "UPDATE identity.credentials SET \"Label\" = COALESCE(\"Label\", \"Nickname\") "
                + "WHERE \"Type\" = 2 AND \"Nickname\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "Nickname",
                schema: "identity",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "RegisteredAttachment",
                schema: "identity",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "RegisteredBrowser",
                schema: "identity",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "RegisteredOs",
                schema: "identity",
                table: "credentials");
        }
    }
}
