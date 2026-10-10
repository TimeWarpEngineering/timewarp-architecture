#region Purpose
// EF Core mapping for a feedback attachment: schema, nullable item link, and indexes.
#endregion

#region Design
// Same schema as FeedbackItem ("feedback"), table "feedback_attachments". The attachment is
// not an aggregate, so Version is mapped for the column and is not a concurrency token.
// FeedbackItemId stays null until submit links the row. Deleting the item cascades to its
// files. The owner index serves the pending-draft count. Columns stay PascalCase.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using TimeWarp.Architecture.Features.Feedback.Domain;

public sealed class FeedbackAttachmentEntityTypeConfiguration : IEntityTypeConfiguration<FeedbackAttachment>
{
  public const string TableName = "feedback_attachments";

  public void Configure(EntityTypeBuilder<FeedbackAttachment> builder)
  {
    builder.ToTable(TableName, FeedbackItemEntityTypeConfiguration.SchemaName);
    builder.HasKey(attachment => attachment.Id);

    builder.Property(attachment => attachment.Id)
      .HasConversion(
        id => id.Value,
        value => FeedbackAttachmentId.From(value))
      .ValueGeneratedNever();

    builder.Property(attachment => attachment.OwnerPrincipalId).IsRequired();
    builder.HasIndex(attachment => attachment.OwnerPrincipalId);

    builder.Property(attachment => attachment.FeedbackItemId)
      .HasConversion(
        id => id.HasValue ? id.Value.Value : (Guid?)null,
        value => value.HasValue ? FeedbackItemId.From(value.Value) : null);
    builder.HasIndex(attachment => attachment.FeedbackItemId);

    builder.HasOne<FeedbackItem>()
      .WithMany()
      .HasForeignKey(attachment => attachment.FeedbackItemId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Property(attachment => attachment.FileName)
      .HasMaxLength(FeedbackAttachment.MaxFileNameLength)
      .IsRequired();

    builder.Property(attachment => attachment.ContentType)
      .HasMaxLength(FeedbackAttachment.MaxContentTypeLength)
      .IsRequired();

    builder.Property(attachment => attachment.Size).IsRequired();

    builder.Property(attachment => attachment.StorageKey)
      .HasMaxLength(80)
      .IsRequired();

    builder.Property(attachment => attachment.UploadedAt).IsRequired();

    builder.Property(attachment => attachment.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
