using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedbackTriage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            schema: "feedback",
            table: "feedback_items",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "New");

        migrationBuilder.CreateTable(
            name: "feedback_activity",
            schema: "feedback",
            columns: table => new
            {
                Sequence = table.Column<int>(type: "integer", nullable: false),
                FeedbackItemId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                AuthorPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feedback_activity", x => new { x.FeedbackItemId, x.Sequence });
                table.ForeignKey(
                    name: "FK_feedback_activity_feedback_items_FeedbackItemId",
                    column: x => x.FeedbackItemId,
                    principalSchema: "feedback",
                    principalTable: "feedback_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_feedback_items_Status_FiledAt",
            schema: "feedback",
            table: "feedback_items",
            columns: new[] { "Status", "FiledAt" });

        // admin.feedback.read / admin.feedback.manage join Administrator's AdminPermissions.
        Insert(migrationBuilder, AdministratorRoleId, AdminFeedbackRead);
        Insert(migrationBuilder, AdministratorRoleId, AdminFeedbackManage);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        Delete(migrationBuilder, AdministratorRoleId, AdminFeedbackManage);
        Delete(migrationBuilder, AdministratorRoleId, AdminFeedbackRead);

        migrationBuilder.DropTable(
            name: "feedback_activity",
            schema: "feedback");

        migrationBuilder.DropIndex(
            name: "IX_feedback_items_Status_FiledAt",
            schema: "feedback",
            table: "feedback_items");

        migrationBuilder.DropColumn(
            name: "Status",
            schema: "feedback",
            table: "feedback_items");
    }

    private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");

    private const string AdminFeedbackRead = "admin.feedback.read";
    private const string AdminFeedbackManage = "admin.feedback.manage";

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
