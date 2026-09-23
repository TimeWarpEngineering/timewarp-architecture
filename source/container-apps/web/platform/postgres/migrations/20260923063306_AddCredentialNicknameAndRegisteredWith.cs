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
