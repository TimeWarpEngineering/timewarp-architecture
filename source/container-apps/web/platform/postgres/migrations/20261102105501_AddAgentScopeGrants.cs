using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261102105501_AddAgentScopeGrants : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "principal_agent_scopes",
            schema: "identity",
            columns: table => new
            {
                PrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                Scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_principal_agent_scopes", x => new { x.PrincipalId, x.Scope });
            });

        migrationBuilder.CreateTable(
            name: "role_agent_scopes",
            schema: "identity",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                Scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_agent_scopes", x => new { x.RoleId, x.Scope });
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "principal_agent_scopes",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "role_agent_scopes",
            schema: "identity");
    }
}
