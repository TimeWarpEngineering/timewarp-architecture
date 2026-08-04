using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "profiles");

            migrationBuilder.CreateTable(
                name: "credentials",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Handle = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicMaterial = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "principal_roles",
                schema: "identity",
                columns: table => new
                {
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_principal_roles", x => new { x.PrincipalId, x.RoleId });
                });

            migrationBuilder.CreateTable(
                name: "principals",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    TrustTier = table.Column<int>(type: "integer", nullable: false),
                    IsQuarantined = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_principals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    Notifications = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credentials_PrincipalId",
                schema: "identity",
                table: "credentials",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_credentials_Type_Handle",
                schema: "identity",
                table: "credentials",
                columns: new[] { "Type", "Handle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credentials",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "principal_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "principals",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "profiles");
        }
    }
}
