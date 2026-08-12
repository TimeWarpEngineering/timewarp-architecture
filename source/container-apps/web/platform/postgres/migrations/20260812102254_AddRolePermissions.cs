using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        // Product RoleIds (role-ids-contracts.cs) — literals keep migrations free of application refs.
        private static readonly Guid MemberRoleId = new("A1B2C3D4-E5F6-4789-A012-3456789ABCDE");
        private static readonly Guid OperatorRoleId = new("B2C3D4E5-F6A7-4890-B123-456789ABCDEF");
        private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");
        private static readonly Guid DeveloperRoleId = new("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
                });

            // Seed mirrors RolePermissionSeed.DefaultGrants (task 182-001).
            SeedRolePermissions(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");
        }

        private static void SeedRolePermissions(MigrationBuilder migrationBuilder)
        {
            // Administrator — all admin.* + self-service
            Insert(migrationBuilder, AdministratorRoleId, "admin.access");
            Insert(migrationBuilder, AdministratorRoleId, "admin.roles.read");
            Insert(migrationBuilder, AdministratorRoleId, "admin.roles.manage");
            Insert(migrationBuilder, AdministratorRoleId, "admin.principals.read");
            Insert(migrationBuilder, AdministratorRoleId, "admin.principals.manage");
            Insert(migrationBuilder, AdministratorRoleId, "profile.read");
            Insert(migrationBuilder, AdministratorRoleId, "settings.read");

            // Member — self-service only
            Insert(migrationBuilder, MemberRoleId, "profile.read");
            Insert(migrationBuilder, MemberRoleId, "settings.read");

            // Developer — developer.* + self-service
            Insert(migrationBuilder, DeveloperRoleId, "developer.access");
            Insert(migrationBuilder, DeveloperRoleId, "developer.claims.read");
            Insert(migrationBuilder, DeveloperRoleId, "profile.read");
            Insert(migrationBuilder, DeveloperRoleId, "settings.read");

            // Operator — self-service until marketplace policies (118)
            Insert(migrationBuilder, OperatorRoleId, "profile.read");
            Insert(migrationBuilder, OperatorRoleId, "settings.read");
        }

        private static void Insert(MigrationBuilder migrationBuilder, Guid roleId, string permissionId)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" },
                values: new object[] { roleId, permissionId });
        }
    }
}
