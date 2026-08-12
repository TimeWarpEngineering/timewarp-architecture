namespace Profile_Model_Mapping_;

using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// Connection-free coverage: the Profile teaching aggregate is on the PostgresDbContext model
/// with table/schema, TypedId key, and Version concurrency token (now supplied by
/// AggregateVersionConvention, not ProfileEntityTypeConfiguration — task 121).
/// </summary>
public class Map
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Map>();

  public static async Task Profile_with_schema_typed_id_and_concurrency_token()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();

    IEntityType entityType = db.Model.FindEntityType(typeof(Profile))
      .ShouldNotBeNull("Profile must be on the PostgresDbContext model");

    entityType.GetSchema().ShouldBe(ProfileEntityTypeConfiguration.SchemaName);
    entityType.GetTableName().ShouldBe(ProfileEntityTypeConfiguration.TableName);

    IProperty id = entityType.FindProperty(nameof(Profile.Id)).ShouldNotBeNull();
    id.ClrType.ShouldBe(typeof(ProfileId));
    id.GetValueConverter().ShouldNotBeNull("ProfileId must convert to a store type (Guid)");

    IProperty version = entityType.FindProperty(nameof(Profile.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
  }

  public static async Task Exposes_profiles_dbset()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    db.Profiles.ShouldNotBeNull();
  }

  private static PostgresDbContext CreateModelOnlyContext()
  {
    // UseNpgsql builds the full model (including Npgsql type mappings) without opening a connection.
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql("Host=127.0.0.1;Database=model-only;Username=unused;Password=unused")
      .Options;
    return new PostgresDbContext(options);
  }
}
