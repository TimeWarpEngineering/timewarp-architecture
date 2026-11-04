using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261104193441_AddRoles : Migration
{
    // Task 473: identity.roles becomes the Role aggregate's root table, and the two grant tables
    // that already exist become its owned rows. Order matters: create the table, seed the four
    // product roles, give every role id already present in a grant table a parent row, and only
    // then add the foreign keys, so existing databases migrate without losing a grant.
    // Product RoleIds (role-ids-contracts.cs) as literals keep migrations free of application refs,
    // the AddRolePermissions precedent. role-migration-seed-tests.cs checks these rows against
    // RoleIds.All and ProductRoleSeed. No HasData: an admin may rename a role, and a model seed
    // would make a later migration overwrite that edit.
    // identity.principal_roles keeps no foreign key (see RoleEntityTypeConfiguration Design).
    private static readonly Guid MemberRoleId = new("A1B2C3D4-E5F6-4789-A012-3456789ABCDE");
    private static readonly Guid OperatorRoleId = new("B2C3D4E5-F6A7-4890-B123-456789ABCDEF");
    private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");
    private static readonly Guid DeveloperRoleId = new("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "roles",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.Id);
            });

        SeedProductRoles(migrationBuilder);

        // Custom roles existed only in the process-memory dictionary, but their grant rows may have
        // reached Postgres. Give each such id a parent so the foreign keys below hold.
        migrationBuilder.Sql(
            """
            INSERT INTO identity.roles ("Id", "Name", "Description", "Version")
            SELECT grants."RoleId",
                   'Role ' || grants."RoleId"::text,
                   'Restored from grants stored before roles were persisted.',
                   0
            FROM (
                SELECT "RoleId" FROM identity.role_permissions
                UNION
                SELECT "RoleId" FROM identity.role_agent_scopes
            ) AS grants
            WHERE NOT EXISTS (SELECT 1 FROM identity.roles AS roles WHERE roles."Id" = grants."RoleId");
            """);

        migrationBuilder.AddForeignKey(
            name: "FK_role_agent_scopes_roles_RoleId",
            schema: "identity",
            table: "role_agent_scopes",
            column: "RoleId",
            principalSchema: "identity",
            principalTable: "roles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_role_permissions_roles_RoleId",
            schema: "identity",
            table: "role_permissions",
            column: "RoleId",
            principalSchema: "identity",
            principalTable: "roles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_role_agent_scopes_roles_RoleId",
            schema: "identity",
            table: "role_agent_scopes");

        migrationBuilder.DropForeignKey(
            name: "FK_role_permissions_roles_RoleId",
            schema: "identity",
            table: "role_permissions");

        migrationBuilder.DropTable(
            name: "roles",
            schema: "identity");
    }

    private static void SeedProductRoles(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "roles",
            columns: ["Id", "Name", "Description", "Version"],
            values: new object[,]
            {
                { MemberRoleId, "Member", "Default human principal after passkey login; self-service only.", 0L },
                { OperatorRoleId, "Operator", "Marketplace and job oversight (agentic shop ops).", 0L },
                { AdministratorRoleId, "Administrator", "Tenant admin: principals, roles, system settings.", 0L },
                { DeveloperRoleId, "Developer", "Template dogfood: demos and diagnostics.", 0L },
            });
    }
}
