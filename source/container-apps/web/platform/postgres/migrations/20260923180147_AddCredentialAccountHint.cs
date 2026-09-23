using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialAccountHint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountHint",
                schema: "identity",
                table: "credentials",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            // Task 250: before this migration an Entra credential's Label held the account
            // (preferred_username, else the name claim). Label is now the provider ("Microsoft 365") and
            // the account is AccountHint. An email-shaped Label is a preferred_username — move it to
            // AccountHint (capped at the column length); a name-claim Label names the person, not the
            // account, and is dropped. Every Entra Label becomes the provider.
            migrationBuilder.Sql(
                "UPDATE identity.credentials SET \"AccountHint\" = LEFT(\"Label\", 256) "
                + "WHERE \"Type\" = 3 AND \"Label\" LIKE '%@%';");
            migrationBuilder.Sql(
                "UPDATE identity.credentials SET \"Label\" = 'Microsoft 365' WHERE \"Type\" = 3;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Inverse of the Up data step: the account goes back into Label where one was captured.
            migrationBuilder.Sql(
                "UPDATE identity.credentials SET \"Label\" = \"AccountHint\" "
                + "WHERE \"Type\" = 3 AND \"AccountHint\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "AccountHint",
                schema: "identity",
                table: "credentials");
        }
    }
}
