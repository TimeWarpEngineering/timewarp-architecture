using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261021214830_AddProtectedSecrets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "secrets");

        migrationBuilder.CreateTable(
            name: "protected_secrets",
            schema: "secrets",
            columns: table => new
            {
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Purpose = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Ciphertext = table.Column<string>(type: "text", nullable: false),
                Suffix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                SetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SetByPrincipalId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_protected_secrets", x => x.Name);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "protected_secrets",
            schema: "secrets");
    }
}
