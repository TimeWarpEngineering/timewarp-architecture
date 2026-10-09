#region Purpose
// Connection-free EF model coverage for FeedbackItem.
#endregion

namespace Feedback_Model_Mapping_;

using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Features.Feedback.Infrastructure;

/// <summary>
/// FeedbackItem is on PostgresDbContext: schema feedback, table feedback_items, TypedId key,
/// string kind, and the aggregate Version concurrency token.
/// </summary>
public class Map
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Map>();

  public static async Task FeedbackItem_with_schema_typed_id_and_concurrency_token()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();

    IEntityType entityType = db.Model.FindEntityType(typeof(FeedbackItem))
      .ShouldNotBeNull("FeedbackItem must be on the PostgresDbContext model");

    entityType.GetSchema().ShouldBe(FeedbackItemEntityTypeConfiguration.SchemaName);
    entityType.GetTableName().ShouldBe(FeedbackItemEntityTypeConfiguration.TableName);

    IProperty id = entityType.FindProperty(nameof(FeedbackItem.Id)).ShouldNotBeNull();
    id.ClrType.ShouldBe(typeof(FeedbackItemId));
    id.GetValueConverter().ShouldNotBeNull("FeedbackItemId must convert to a store type (Guid)");

    IProperty kind = entityType.FindProperty(nameof(FeedbackItem.Kind)).ShouldNotBeNull();
    kind.GetMaxLength().ShouldBe(32);

    IProperty title = entityType.FindProperty(nameof(FeedbackItem.Title)).ShouldNotBeNull();
    title.GetMaxLength().ShouldBe(FeedbackItem.MaxTitleLength);

    IProperty body = entityType.FindProperty(nameof(FeedbackItem.Body)).ShouldNotBeNull();
    body.GetMaxLength().ShouldBe(FeedbackItem.MaxBodyLength);

    IProperty version = entityType.FindProperty(nameof(FeedbackItem.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
  }

  public static async Task Exposes_feedback_items_dbset()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    db.FeedbackItems.ShouldNotBeNull();
  }

  private static PostgresDbContext CreateModelOnlyContext()
  {
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql("Host=127.0.0.1;Database=model-only;Username=unused;Password=unused")
      .Options;
    return new PostgresDbContext(options);
  }
}
