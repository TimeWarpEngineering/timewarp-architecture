namespace Identity_Model_Mapping_;

using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Architecture.Features.Identity.Infrastructure;
using TimeWarp.Identity;

/// <summary>
/// Connection-free coverage: Principal and Credential are on the PostgresDbContext model with
/// schema identity, TypedId keys, Version concurrency tokens, bytea field access, and unique
/// (Type, Handle) — no Docker required.
/// </summary>
public class Map
{
  public void Principal_with_schema_typed_id_and_concurrency_token()
  {
    using PostgresDbContext db = CreateModelOnlyContext();

    IEntityType entityType = db.Model.FindEntityType(typeof(Principal))
      .ShouldNotBeNull("Principal must be on the PostgresDbContext model");

    entityType.GetSchema().ShouldBe(PrincipalEntityTypeConfiguration.SchemaName);
    entityType.GetTableName().ShouldBe(PrincipalEntityTypeConfiguration.TableName);

    IProperty id = entityType.FindProperty(nameof(Principal.Id)).ShouldNotBeNull();
    id.ClrType.ShouldBe(typeof(PrincipalId));
    id.GetValueConverter().ShouldNotBeNull("PrincipalId must convert to a store type (Guid)");

    IProperty version = entityType.FindProperty(nameof(Principal.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
  }

  public void Credential_with_bytea_field_access_unique_handle_and_concurrency_token()
  {
    using PostgresDbContext db = CreateModelOnlyContext();

    IEntityType entityType = db.Model.FindEntityType(typeof(Credential))
      .ShouldNotBeNull("Credential must be on the PostgresDbContext model");

    entityType.GetSchema().ShouldBe(CredentialEntityTypeConfiguration.SchemaName);
    entityType.GetTableName().ShouldBe(CredentialEntityTypeConfiguration.TableName);

    IProperty id = entityType.FindProperty(nameof(Credential.Id)).ShouldNotBeNull();
    id.ClrType.ShouldBe(typeof(CredentialId));
    id.GetValueConverter().ShouldNotBeNull();

    IProperty handle = entityType.FindProperty(nameof(Credential.Handle)).ShouldNotBeNull();
    handle.GetFieldName().ShouldBe("HandleField");
    handle.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Field);

    IProperty material = entityType.FindProperty(nameof(Credential.PublicMaterial)).ShouldNotBeNull();
    material.GetFieldName().ShouldBe("PublicMaterialField");
    material.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Field);

    IProperty version = entityType.FindProperty(nameof(Credential.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();

    IIndex? uniqueHandle = entityType.GetIndexes()
      .SingleOrDefault(index => index.IsUnique
        && index.Properties.Count == 2
        && index.Properties[0].Name == nameof(Credential.Type)
        && index.Properties[1].Name == nameof(Credential.Handle));
    uniqueHandle.ShouldNotBeNull("Unique index on (Type, Handle) is required for handle uniqueness");
  }

  public void Exposes_principals_and_credentials_dbsets()
  {
    using PostgresDbContext db = CreateModelOnlyContext();
    db.Principals.ShouldNotBeNull();
    db.Credentials.ShouldNotBeNull();
  }

  private static PostgresDbContext CreateModelOnlyContext()
  {
    DbContextOptions<PostgresDbContext> options = new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql("Host=127.0.0.1;Database=model-only;Username=unused;Password=unused")
      .Options;
    return new PostgresDbContext(options);
  }
}
