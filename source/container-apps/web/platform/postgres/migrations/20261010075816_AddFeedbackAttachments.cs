using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedbackAttachments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "feedback_attachments",
            schema: "feedback",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OwnerPrincipalId = table.Column<Guid>(type: "uuid", nullable: false),
                FeedbackItemId = table.Column<Guid>(type: "uuid", nullable: true),
                FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ContentType = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                StorageKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feedback_attachments", x => x.Id);
                table.ForeignKey(
                    name: "FK_feedback_attachments_feedback_items_FeedbackItemId",
                    column: x => x.FeedbackItemId,
                    principalSchema: "feedback",
                    principalTable: "feedback_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_feedback_attachments_FeedbackItemId",
            schema: "feedback",
            table: "feedback_attachments",
            column: "FeedbackItemId");

        migrationBuilder.CreateIndex(
            name: "IX_feedback_attachments_OwnerPrincipalId",
            schema: "feedback",
            table: "feedback_attachments",
            column: "OwnerPrincipalId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "feedback_attachments",
            schema: "feedback");
    }
}
