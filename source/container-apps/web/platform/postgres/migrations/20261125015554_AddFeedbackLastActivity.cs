using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261125015554_AddFeedbackLastActivity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastActivityAt",
            schema: "feedback",
            table: "feedback_items",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        // Task 520: existing rows take the At of their last thread entry (highest Sequence),
        // or FiledAt when the thread is empty. That is the value FeedbackItem keeps for new
        // writes, so the aggregate's LastActivityAt invariant holds for migrated rows.
        migrationBuilder.Sql(
            """
            UPDATE feedback.feedback_items AS item
            SET "LastActivityAt" = COALESCE(
              (
                SELECT activity."At"
                FROM feedback.feedback_activity AS activity
                WHERE activity."FeedbackItemId" = item."Id"
                ORDER BY activity."Sequence" DESC
                LIMIT 1
              ),
              item."FiledAt");
            """);

        migrationBuilder.CreateIndex(
            name: "IX_feedback_items_LastActivityAt",
            schema: "feedback",
            table: "feedback_items",
            column: "LastActivityAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_feedback_items_LastActivityAt",
            schema: "feedback",
            table: "feedback_items");

        migrationBuilder.DropColumn(
            name: "LastActivityAt",
            schema: "feedback",
            table: "feedback_items");
    }
}
