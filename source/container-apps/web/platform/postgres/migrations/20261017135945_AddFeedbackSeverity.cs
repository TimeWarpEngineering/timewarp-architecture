using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedbackSeverity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Severity",
            schema: "feedback",
            table: "feedback_items",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Unset");

        migrationBuilder.AddColumn<string>(
            name: "FromSeverity",
            schema: "feedback",
            table: "feedback_activity",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ToSeverity",
            schema: "feedback",
            table: "feedback_activity",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        // feedback.read/note/severity/status join Administrator beside admin.feedback.* (task 378).
        // They are not admin.* and are not protected core. Member, Developer, and Operator do not receive them.
        migrationBuilder.InsertData(
            schema: "identity",
            table: "role_permissions",
            columns: ["RoleId", "PermissionId"],
            values: new object[,]
            {
                { AdministratorRoleId, FeedbackRead },
                { AdministratorRoleId, FeedbackNote },
                { AdministratorRoleId, FeedbackSeverity },
                { AdministratorRoleId, FeedbackStatus },
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Severity",
            schema: "feedback",
            table: "feedback_items");

        migrationBuilder.DropColumn(
            name: "FromSeverity",
            schema: "feedback",
            table: "feedback_activity");

        migrationBuilder.DropColumn(
            name: "ToSeverity",
            schema: "feedback",
            table: "feedback_activity");

        migrationBuilder.DeleteData(
            schema: "identity",
            table: "role_permissions",
            keyColumns: ["RoleId", "PermissionId"],
            keyValues: new object[,]
            {
                { AdministratorRoleId, FeedbackRead },
                { AdministratorRoleId, FeedbackNote },
                { AdministratorRoleId, FeedbackSeverity },
                { AdministratorRoleId, FeedbackStatus },
            });
    }

    private static readonly Guid AdministratorRoleId = new("834B9073-D5FF-40B3-938A-968C23FA76CC");

    private const string FeedbackRead = "feedback.read";
    private const string FeedbackNote = "feedback.note";
    private const string FeedbackSeverity = "feedback.severity";
    private const string FeedbackStatus = "feedback.status";
}
