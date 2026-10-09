using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261009001245_AddFeedbackItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "feedback");

        migrationBuilder.CreateTable(
            name: "feedback_items",
            schema: "feedback",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                FiledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feedback_items", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_feedback_items_OwnerPrincipalId",
            schema: "feedback",
            table: "feedback_items",
            column: "OwnerPrincipalId");

        // feedback.file.self joins SelfServicePermissions for every product role.
        Insert(migrationBuilder, AdministratorRoleId, FeedbackFileSelf);
        Insert(migrationBuilder, MemberRoleId, FeedbackFileSelf);
        Insert(migrationBuilder, DeveloperRoleId, FeedbackFileSelf);
        Insert(migrationBuilder, OperatorRoleId, FeedbackFileSelf);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        Delete(migrationBuilder, AdministratorRoleId, FeedbackFileSelf);
        Delete(migrationBuilder, MemberRoleId, FeedbackFileSelf);
        Delete(migrationBuilder, DeveloperRoleId, FeedbackFileSelf);
        Delete(migrationBuilder, OperatorRoleId, FeedbackFileSelf);

        migrationBuilder.DropTable(
            name: "feedback_items",
            schema: "feedback");
    }

    private static readonly Guid MemberRoleId = new("A1B2C3D4-E5F6-4789-A012-3456789ABCDE");
    private static readonly Guid OperatorRoleId = new("B2C3D4E5-F6A7-4890-B123-456789ABCDEF");
    private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");
    private static readonly Guid DeveloperRoleId = new("80EE3E0C-A8B6-45D6-BA27-7DEE2691AA42");

    private const string FeedbackFileSelf = "feedback.file.self";

    private static void Insert(MigrationBuilder migrationBuilder, Guid roleId, string permissionId)
    {
        migrationBuilder.InsertData(
            schema: "identity",
            table: "role_permissions",
            columns: ["RoleId", "PermissionId"],
            values: [roleId, permissionId]);
    }

    private static void Delete(MigrationBuilder migrationBuilder, Guid roleId, string permissionId)
    {
        migrationBuilder.DeleteData(
            schema: "identity",
            table: "role_permissions",
            keyColumns: ["RoleId", "PermissionId"],
            keyValues: [roleId, permissionId]);
    }
}
