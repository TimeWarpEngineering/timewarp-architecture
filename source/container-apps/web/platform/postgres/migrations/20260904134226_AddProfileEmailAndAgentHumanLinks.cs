using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileEmailAndAgentHumanLinks : Migration
    {
        // Product RoleIds (role-ids-contracts.cs) — literals keep migrations free of application refs.
        private static readonly Guid MemberRoleId = new("A1B2C3D4-E5F6-4789-A012-3456789ABCDE");
        private static readonly Guid OperatorRoleId = new("B2C3D4E5-F6A7-4890-B123-456789ABCDEF");
        private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");
        private static readonly Guid DeveloperRoleId = new("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

        private const string ProfileWrite = "profile.write";
        private const string AgentLinkManageSelf = "agent-link.manage.self";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agent_links");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "profiles",
                table: "profiles",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agent_links",
                schema: "agent_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    HumanPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_links", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_links_AgentPrincipalId_HumanPrincipalId",
                schema: "agent_links",
                table: "agent_links",
                columns: new[] { "AgentPrincipalId", "HumanPrincipalId" });

            // Task 205: SelfServicePermissions gains profile.write and agent-link.manage.self.
            InsertSelfService(migrationBuilder, AdministratorRoleId);
            InsertSelfService(migrationBuilder, MemberRoleId);
            InsertSelfService(migrationBuilder, DeveloperRoleId);
            InsertSelfService(migrationBuilder, OperatorRoleId);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DeleteSelfService(migrationBuilder, AdministratorRoleId);
            DeleteSelfService(migrationBuilder, MemberRoleId);
            DeleteSelfService(migrationBuilder, DeveloperRoleId);
            DeleteSelfService(migrationBuilder, OperatorRoleId);

            migrationBuilder.DropTable(
                name: "agent_links",
                schema: "agent_links");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "profiles",
                table: "profiles");
        }

        private static void InsertSelfService(MigrationBuilder migrationBuilder, Guid roleId)
        {
            Insert(migrationBuilder, roleId, ProfileWrite);
            Insert(migrationBuilder, roleId, AgentLinkManageSelf);
        }

        private static void DeleteSelfService(MigrationBuilder migrationBuilder, Guid roleId)
        {
            Delete(migrationBuilder, roleId, ProfileWrite);
            Delete(migrationBuilder, roleId, AgentLinkManageSelf);
        }

        private static void Insert(MigrationBuilder migrationBuilder, Guid roleId, string permissionId)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" },
                values: new object[] { roleId, permissionId });
        }

        private static void Delete(MigrationBuilder migrationBuilder, Guid roleId, string permissionId)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumns: new[] { "RoleId", "PermissionId" },
                keyValues: new object[] { roleId, permissionId });
        }
    }
}
