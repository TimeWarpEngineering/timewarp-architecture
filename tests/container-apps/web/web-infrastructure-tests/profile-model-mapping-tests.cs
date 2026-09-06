#region Purpose
// Connection-free EF model coverage for Profile (email column) and AgentHumanLink (task 205).
#endregion

namespace Profile_Model_Mapping_;

using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Architecture.Features.AgentLinks.Infrastructure;

/// <summary>
/// Connection-free coverage: the Profile teaching aggregate is on the PostgresDbContext model
/// with table/schema, TypedId key, optional Email, and Version concurrency token (now supplied by
/// AggregateVersionConvention, not ProfileEntityTypeConfiguration — task 121). AgentHumanLink
/// is the optional agent↔human product aggregate (schema agent_links).
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

    IProperty email = entityType.FindProperty(nameof(Profile.Email)).ShouldNotBeNull();
    email.IsNullable.ShouldBeTrue();
    email.GetMaxLength().ShouldBe(Profile.MaxEmailLength);

    IProperty version = entityType.FindProperty(nameof(Profile.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
  }

  public static async Task Exposes_profiles_dbset()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    db.Profiles.ShouldNotBeNull();
  }

  public static async Task Exposes_agent_human_links_dbset()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();
    db.AgentHumanLinks.ShouldNotBeNull();
  }

  public static async Task AgentHumanLink_with_schema_typed_id_and_concurrency_token()
  {
    await using PostgresDbContext db = CreateModelOnlyContext();

    IEntityType entityType = db.Model.FindEntityType(typeof(AgentHumanLink))
      .ShouldNotBeNull("AgentHumanLink must be on the PostgresDbContext model");

    entityType.GetSchema().ShouldBe(AgentHumanLinkEntityTypeConfiguration.SchemaName);
    entityType.GetTableName().ShouldBe(AgentHumanLinkEntityTypeConfiguration.TableName);

    IProperty id = entityType.FindProperty(nameof(AgentHumanLink.Id)).ShouldNotBeNull();
    id.ClrType.ShouldBe(typeof(AgentHumanLinkId));
    id.GetValueConverter().ShouldNotBeNull("AgentHumanLinkId must convert to a store type (Guid)");

    IProperty version = entityType.FindProperty(nameof(AgentHumanLink.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
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
