#region Purpose
// EF Core mapping for a feedback filing: schema, TypedId key, kind as a string, owner index.
#endregion

#region Design
// Schema-per-slice on PostgresDbContext: schema "feedback", table "feedback_items". Kind is stored
// as the enum name so a numeric reorder cannot change meaning. Version's concurrency token comes
// from AggregateVersionConvention; this configuration does not call IsConcurrencyToken.
// The owner index serves the filer's list. There is no cross-owner query.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using TimeWarp.Architecture.Features.Feedback.Domain;

public sealed class FeedbackItemEntityTypeConfiguration : IEntityTypeConfiguration<FeedbackItem>
{
  public const string SchemaName = "feedback";
  public const string TableName = "feedback_items";

  public void Configure(EntityTypeBuilder<FeedbackItem> builder)
  {
    builder.ToTable(TableName, SchemaName);
    builder.HasKey(item => item.Id);

    builder.Property(item => item.Id)
      .HasConversion(
        id => id.Value,
        value => FeedbackItemId.From(value))
      .ValueGeneratedNever();

    builder.Property(item => item.OwnerPrincipalId).IsRequired();
    builder.HasIndex(item => item.OwnerPrincipalId);

    builder.Property(item => item.Kind)
      .HasConversion<string>()
      .HasMaxLength(32)
      .IsRequired();

    builder.Property(item => item.Title)
      .HasMaxLength(FeedbackItem.MaxTitleLength)
      .IsRequired();

    builder.Property(item => item.Body)
      .HasMaxLength(FeedbackItem.MaxBodyLength)
      .IsRequired();

    builder.Property(item => item.FiledAt).IsRequired();

    builder.Property(item => item.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
