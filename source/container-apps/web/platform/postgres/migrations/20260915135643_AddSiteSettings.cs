using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        // Product RoleIds (role-ids-contracts.cs) — literals keep migrations free of application refs.
        private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");

        private const string SettingsWrite = "settings.write";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_settings",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntraSignInEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EntraAllowBootstrap = table.Column<bool>(type: "boolean", nullable: false),
                    EntraTrustedTenants = table.Column<string>(type: "jsonb", nullable: false),
                    PasskeyPromptMode = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_settings", x => x.Id);
                });

            // Task 219-006: Administrator seed gains settings.write (not self-service).
            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" },
                values: new object[] { AdministratorRoleId, SettingsWrite });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { AdministratorRoleId, SettingsWrite });

            migrationBuilder.DropTable(
                name: "site_settings",
                schema: "identity");
        }
    }
}
