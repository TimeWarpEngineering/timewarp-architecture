using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAdminProfilesReadPermission : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // admin.profiles.read joins Administrator's AdminPermissions (task 328).
        migrationBuilder.InsertData(
            schema: "identity",
            table: "role_permissions",
            columns: ["RoleId", "PermissionId"],
            values: [AdministratorRoleId, AdminProfilesRead]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            schema: "identity",
            table: "role_permissions",
            keyColumns: ["RoleId", "PermissionId"],
            keyValues: [AdministratorRoleId, AdminProfilesRead]);
    }

    private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");

    private const string AdminProfilesRead = "admin.profiles.read";
}
