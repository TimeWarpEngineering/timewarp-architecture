#region Purpose
// EF Core mapping for a feedback filing: schema, TypedId key, kind as a string, owner index.
#endregion

#region Design
// Schema-per-slice on PostgresDbContext: schema "feedback", table "feedback_items". Kind is stored
// as the enum name so a numeric reorder cannot change meaning. Version's concurrency token comes
// from AggregateVersionConvention; this configuration does not call IsConcurrencyToken.
// The owner index serves the filer's list. The (Status, FiledAt) index serves the admin
// cross-owner list, which filters by status and orders newest first (task 314). Status is
// stored as the enum name like Kind; existing rows default to New in the migration.
// The thread is OwnsMany into "feedback_activity", keyed by (FeedbackItemId, Sequence) so two
// writers appending the same position collide. Field access on ActivityList because the
// Activity getter returns a sorted copy. IsVisibleToFiler is derived and not mapped.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Infrastructure;

using TimeWarp.Architecture.Features.Feedback.Domain;

public sealed class FeedbackItemEntityTypeConfiguration : IEntityTypeConfiguration<FeedbackItem>
{
  public const string SchemaName = "feedback";
  public const string TableName = "feedback_items";
  public const string ActivityTableName = "feedback_activity";
  private const string FeedbackItemIdColumn = "FeedbackItemId";

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

    builder.Property(item => item.Status)
      .HasConversion<string>()
      .HasMaxLength(32)
      .IsRequired();
    builder.HasIndex(item => new { item.Status, item.FiledAt });

    builder.OwnsMany(item => item.Activity, activity =>
    {
      activity.ToTable(ActivityTableName, SchemaName);
      activity.WithOwner().HasForeignKey(FeedbackItemIdColumn);
      activity.Property<FeedbackItemId>(FeedbackItemIdColumn)
        .HasConversion(
          id => id.Value,
          value => FeedbackItemId.From(value));
      activity.HasKey(FeedbackItemIdColumn, nameof(FeedbackActivity.Sequence));
      activity.Property(entry => entry.Sequence).ValueGeneratedNever();
      activity.Property(entry => entry.Kind)
        .HasConversion<string>()
        .HasMaxLength(32)
        .IsRequired();
      activity.Property(entry => entry.AuthorPrincipalId).IsRequired();
      activity.Property(entry => entry.Body).HasMaxLength(FeedbackItem.MaxMessageLength);
      activity.Property(entry => entry.FromStatus).HasConversion<string>().HasMaxLength(32);
      activity.Property(entry => entry.ToStatus).HasConversion<string>().HasMaxLength(32);
      activity.Property(entry => entry.At).IsRequired();
      activity.Ignore(entry => entry.IsVisibleToFiler);
    });
    builder.Navigation(item => item.Activity)
      .HasField("ActivityList")
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.Property(item => item.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);
  }
}
