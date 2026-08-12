using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialManageSelfPermission : Migration
    {
        // Product RoleIds (role-ids-contracts.cs) — literals keep migrations free of application refs.
        private static readonly Guid MemberRoleId = new("A1B2C3D4-E5F6-4789-A012-3456789ABCDE");
        private static readonly Guid OperatorRoleId = new("B2C3D4E5-F6A7-4890-B123-456789ABCDEF");
        private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");
        private static readonly Guid DeveloperRoleId = new("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

        private const string CredentialManageSelf = "credential.manage.self";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Task 182-006: SelfServicePermissions gains credential.manage.self for all product roles.
            Insert(migrationBuilder, AdministratorRoleId, CredentialManageSelf);
            Insert(migrationBuilder, MemberRoleId, CredentialManageSelf);
            Insert(migrationBuilder, DeveloperRoleId, CredentialManageSelf);
            Insert(migrationBuilder, OperatorRoleId, CredentialManageSelf);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Delete(migrationBuilder, AdministratorRoleId, CredentialManageSelf);
            Delete(migrationBuilder, MemberRoleId, CredentialManageSelf);
            Delete(migrationBuilder, DeveloperRoleId, CredentialManageSelf);
            Delete(migrationBuilder, OperatorRoleId, CredentialManageSelf);
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
