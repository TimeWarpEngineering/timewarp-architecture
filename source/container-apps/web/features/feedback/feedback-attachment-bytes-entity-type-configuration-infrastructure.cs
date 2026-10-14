#region Purpose
// EF Core mapping for feedback attachment bytes: one bytea row per storage key.
#endregion

#region Design
// Same schema as FeedbackItem ("feedback"), table "feedback_attachment_bytes". The key is the
// storage key, with the same 80-character bound as FeedbackAttachment.StorageKey. Content is
// bytea; FeedbackAttachmentRules.MaxBytes (5 MiB) bounds it, enforced by upload before any
// store is touched. Postgres TOASTs values this size out of line, so the attachment row
// queries never read them. Columns stay PascalCase.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

public sealed class FeedbackAttachmentBytesEntityTypeConfiguration : IEntityTypeConfiguration<FeedbackAttachmentBytes>
{
  public const string TableName = "feedback_attachment_bytes";

  public void Configure(EntityTypeBuilder<FeedbackAttachmentBytes> builder)
  {
    builder.ToTable(TableName, FeedbackItemEntityTypeConfiguration.SchemaName);
    builder.HasKey(bytes => bytes.StorageKey);

    builder.Property(bytes => bytes.StorageKey)
      .HasMaxLength(FeedbackAttachmentEntityTypeConfiguration.StorageKeyMaxLength)
      .ValueGeneratedNever();

    builder.Property(bytes => bytes.Content)
      .HasColumnType("bytea")
      .IsRequired();
  }
}
