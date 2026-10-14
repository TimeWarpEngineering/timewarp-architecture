using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeWarp.Architecture.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedbackAttachmentBytes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Attachment bytes beside their rows when no Azure Blob connection string is set (task 352).
        // Rows filed before this migration keep no bytes here; their download is a 404.
        migrationBuilder.CreateTable(
            name: "feedback_attachment_bytes",
            schema: "feedback",
            columns: table => new
            {
                StorageKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Content = table.Column<byte[]>(type: "bytea", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feedback_attachment_bytes", x => x.StorageKey);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "feedback_attachment_bytes",
            schema: "feedback");
    }
}
