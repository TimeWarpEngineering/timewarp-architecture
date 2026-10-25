using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261025144841_ConvertEntraAcceptedTenants : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "EntraAcceptedTenantIds",
            schema: "identity",
            table: "site_settings",
            newName: "EntraAcceptedTenants");

        // A GUID-string array becomes {tenantId, domain:null}. An object array is already the new shape.
        migrationBuilder.Sql(
            """
            UPDATE identity.site_settings
            SET "EntraAcceptedTenants" = COALESCE(
              (
                SELECT jsonb_agg(jsonb_build_object('tenantId', element, 'domain', NULL))
                FROM jsonb_array_elements_text("EntraAcceptedTenants") AS element
              ),
              '[]'::jsonb)
            WHERE jsonb_typeof("EntraAcceptedTenants") = 'array'
              AND jsonb_array_length("EntraAcceptedTenants") > 0
              AND jsonb_typeof("EntraAcceptedTenants" -> 0) = 'string';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The old column is a GUID array. Domains are display-only and are dropped on the way back.
        migrationBuilder.Sql(
            """
            UPDATE identity.site_settings
            SET "EntraAcceptedTenants" = COALESCE(
              (
                SELECT jsonb_agg(element -> 'tenantId')
                FROM jsonb_array_elements("EntraAcceptedTenants") AS element
              ),
              '[]'::jsonb)
            WHERE jsonb_typeof("EntraAcceptedTenants") = 'array'
              AND jsonb_array_length("EntraAcceptedTenants") > 0
              AND jsonb_typeof("EntraAcceptedTenants" -> 0) = 'object';
            """);

        migrationBuilder.RenameColumn(
            name: "EntraAcceptedTenants",
            schema: "identity",
            table: "site_settings",
            newName: "EntraAcceptedTenantIds");
    }
}
