// EF IRoleStore and the two role grant read ports over the aggregate's rows (task 473).
// Ephemeral Postgres per test (env or Testcontainers); soft-skip interactively, fail closed in CI.

namespace RoleStore_.Ef;

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles.Domain;
using TimeWarp.Architecture.Features.Admin.Roles.Infrastructure;
using TimeWarp.Architecture.Testing;
using TimeWarp.Foundation.Application.Exceptions;

file static class RoleDatabase
{
  private const string Prefix = "ef_roles_";

  private static readonly Lazy<Task<PostgresTestAvailability>> Availability =
    new(() => PostgresTestAvailability.ResolveAsync("timewarp_role_tests"), LazyThreadSafetyMode.ExecutionAndPublication);

  public static async Task<bool> SkipIfUnavailableAsync()
  {
    PostgresTestAvailability availability = await Availability.Value;
    if (availability.AdminConnectionString is not null)
    {
      return false;
    }

    if (PostgresTestAvailability.IsCiEnvironment())
    {
      throw new InvalidOperationException(
        "EfRoleStore tests require a connection string or Docker under CI. " + (availability.SkipReason ?? "no connection"));
    }

    Console.WriteLine($"[SKIP] RoleStore_.Ef: {availability.SkipReason ?? "no connection"}");
    return true;
  }

  /// <summary>A fresh database (optionally migrated only up to <paramref name="targetMigration"/>).</summary>
  public static async Task<string> CreateAsync(string? targetMigration = null)
  {
    PostgresTestAvailability availability = await Availability.Value;
    string admin = availability.AdminConnectionString ?? throw new InvalidOperationException("no connection");
    string databaseName = Prefix + Guid.NewGuid().ToString("N");

    await using (NpgsqlConnection connection = new(admin))
    {
      await connection.OpenAsync();
      await using NpgsqlCommand command = connection.CreateCommand();
#pragma warning disable CA2100 // name is minted above from a Guid, not input
      command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
#pragma warning restore CA2100
      await command.ExecuteNonQueryAsync();
    }

    string connectionString = new NpgsqlConnectionStringBuilder(admin) { Database = databaseName }.ConnectionString;
    await using PostgresDbContext db = Context(connectionString);
    await db.GetService<IMigrator>().MigrateAsync(targetMigration);
    return connectionString;
  }

  public static PostgresDbContext Context(string connectionString) =>
    new(new DbContextOptionsBuilder<PostgresDbContext>()
      .UseNpgsql(connectionString, PostgresRetryPolicy.Configure)
      .Options);
}

public class Role_store
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Role_store>();

  public static async Task Migration_seeds_the_product_roles_with_their_permissions()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync();

    await using PostgresDbContext db = RoleDatabase.Context(connection);
    IReadOnlyList<Role> roles = await new EfRoleStore(db).ListAsync();

    roles.Select(role => role.Id.Value).Order().ShouldBe(RoleIds.All.Order());
    roles.Single(role => role.Id.Value == RoleIds.Member).Name.ShouldBe("Member");
    Role administrator = roles.Single(role => role.Id.Value == RoleIds.Administrator);
    administrator.Permissions.Select(permission => permission.PermissionId).Order(StringComparer.Ordinal)
      .ShouldBe(RolePermissionSeed.DefaultGrants[RoleIds.Administrator].Order(StringComparer.Ordinal));
    roles.ShouldAllBe(role => role.AgentScopes.Count == 0);
  }

  public static async Task Add_then_find_round_trips_details_and_children()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync();

    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read", "admin.access"]);
    role.SetAgentScopes(["identity:read"]);
    await using (PostgresDbContext write = RoleDatabase.Context(connection))
    {
      await new EfRoleStore(write).AddAsync(role);
      await Should.ThrowAsync<InvalidOperationException>(() => new EfRoleStore(write).AddAsync(Role.Create(role.Id, "Again", "Duplicate.")));
    }

    await using PostgresDbContext read = RoleDatabase.Context(connection);
    Role found = (await new EfRoleStore(read).FindAsync(role.Id)).ShouldNotBeNull();
    found.Name.ShouldBe("Auditor");
    found.Description.ShouldBe("Read-only access.");
    found.Permissions.Select(permission => permission.PermissionId).Order(StringComparer.Ordinal).ShouldBe(["admin.access", "profile.read"]);
    found.AgentScopes.Select(scope => scope.Scope).ShouldBe(["identity:read"]);
    (await new EfRoleStore(read).FindAsync(RoleId.New())).ShouldBeNull();
    (await new EfRoleStore(read).FindManyAsync([role.Id, RoleId.New()])).Select(each => each.Id).ShouldBe([role.Id]);
  }

  public static async Task Update_replaces_children_and_bumps_version()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync();

    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read", "settings.read"]);
    role.SetAgentScopes(["identity:read"]);
    await using (PostgresDbContext write = RoleDatabase.Context(connection))
    {
      await new EfRoleStore(write).AddAsync(role);
    }

    await using (PostgresDbContext update = RoleDatabase.Context(connection))
    {
      EfRoleStore store = new(update);
      Role loaded = (await store.FindAsync(role.Id)).ShouldNotBeNull();
      loaded.UpdateDetails("Reviewer", "Reviews things.");
      loaded.SetPermissions(["settings.read", "admin.access"]);
      loaded.SetAgentScopes([]);
      await store.UpdateAsync(loaded);
    }

    await using PostgresDbContext read = RoleDatabase.Context(connection);
    Role found = (await new EfRoleStore(read).FindAsync(role.Id)).ShouldNotBeNull();
    found.Name.ShouldBe("Reviewer");
    found.Permissions.Select(permission => permission.PermissionId).Order(StringComparer.Ordinal).ShouldBe(["admin.access", "settings.read"]);
    found.AgentScopes.ShouldBeEmpty();
    found.Version.ShouldBe(1);
    await Should.ThrowAsync<InvalidOperationException>(() => new EfRoleStore(read).UpdateAsync(Role.Create("Ghost", "Not stored.")));
  }

  public static async Task Remove_deletes_a_custom_role_and_its_rows_but_refuses_a_product_role()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync();

    var role = Role.Create("Auditor", "Read-only access.");
    role.SetPermissions(["profile.read"]);
    role.SetAgentScopes(["identity:read"]);
    await using (PostgresDbContext write = RoleDatabase.Context(connection))
    {
      await new EfRoleStore(write).AddAsync(role);
    }

    await using (PostgresDbContext remove = RoleDatabase.Context(connection))
    {
      await new EfRoleStore(remove).RemoveAsync(role.Id);
    }

    await using (PostgresDbContext guard = RoleDatabase.Context(connection))
    {
      await Should.ThrowAsync<InvalidOperationException>(() => new EfRoleStore(guard).RemoveAsync(RoleId.From(RoleIds.Administrator)));
    }

    await using PostgresDbContext read = RoleDatabase.Context(connection);
    (await new EfRoleStore(read).FindAsync(role.Id)).ShouldBeNull();
    (await new EfRolePermissionStore(read).GetPermissionIdsForRoleAsync(role.Id.Value)).ShouldBeEmpty();
    (await new EfRoleAgentScopeStore(read).GetScopesForRoleAsync(role.Id.Value)).ShouldBeEmpty();
    (await new EfRoleStore(read).FindAsync(RoleId.From(RoleIds.Administrator))).ShouldNotBeNull();
  }

  public static async Task Save_runs_the_role_invariants()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync();

    await using PostgresDbContext db = RoleDatabase.Context(connection);
    Role member = await db.Roles.SingleAsync(role => role.Id == RoleId.From(RoleIds.Member));
    var permissions = (List<PermissionGrant>)typeof(Role)
      .GetField("PermissionList", BindingFlags.NonPublic | BindingFlags.Instance)!
      .GetValue(member)!;
    permissions.Add(new PermissionGrant("retired.permission"));

    await Should.ThrowAsync<DomainInvariantViolationException>(() => db.SaveChangesAsync());
  }
}

public class Role_grant_read_ports
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Role_grant_read_ports>();

  public static async Task Read_the_aggregate_rows_and_omit_roles_without_scopes()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync();

    await using (PostgresDbContext write = RoleDatabase.Context(connection))
    {
      EfRoleStore store = new(write);
      Role developer = (await store.FindAsync(RoleId.From(RoleIds.Developer))).ShouldNotBeNull();
      developer.SetAgentScopes(["feedback:read", "identity:read"]);
      await store.UpdateAsync(developer);
    }

    await using PostgresDbContext read = RoleDatabase.Context(connection);
    (await new EfRolePermissionStore(read).GetPermissionIdsForRoleAsync(RoleIds.Member)).Order(StringComparer.Ordinal)
      .ShouldBe(RolePermissionSeed.DefaultGrants[RoleIds.Member].Order(StringComparer.Ordinal));
    (await new EfRolePermissionStore(read).GetPermissionIdsForRoleAsync(Guid.NewGuid())).ShouldBeEmpty();

    EfRoleAgentScopeStore scopes = new(read);
    (await scopes.GetScopesForRoleAsync(RoleIds.Developer)).Order(StringComparer.Ordinal).ShouldBe(["feedback:read", "identity:read"]);
    IReadOnlyDictionary<Guid, IReadOnlyList<string>> batch = await scopes.GetScopesForRolesAsync([RoleIds.Developer, RoleIds.Member]);
    batch.Keys.ShouldBe([RoleIds.Developer]);
    (await scopes.GetScopesForRolesAsync([])).ShouldBeEmpty();
  }
}

public class Add_roles_migration
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Add_roles_migration>();

  public static async Task Gives_stray_grant_role_ids_a_parent_before_adding_the_foreign_keys()
  {
    if (await RoleDatabase.SkipIfUnavailableAsync()) return;
    string connection = await RoleDatabase.CreateAsync("20261102105501_AddAgentScopeGrants");
    Guid stray = Guid.NewGuid();
    Guid strayScopeOnly = Guid.NewGuid();

    await using (PostgresDbContext before = RoleDatabase.Context(connection))
    {
      await before.Database.ExecuteSqlAsync(
        $"""INSERT INTO identity.role_permissions ("RoleId", "PermissionId") VALUES ({stray}, 'profile.read')""");
      await before.Database.ExecuteSqlAsync(
        $"""INSERT INTO identity.role_agent_scopes ("RoleId", "Scope") VALUES ({stray}, 'identity:read'), ({strayScopeOnly}, 'demo:invoke')""");
      await before.GetService<IMigrator>().MigrateAsync();
    }

    await using PostgresDbContext after = RoleDatabase.Context(connection);
    EfRoleStore store = new(after);
    Role restored = (await store.FindAsync(RoleId.From(stray))).ShouldNotBeNull();
    restored.Permissions.Select(permission => permission.PermissionId).ShouldBe(["profile.read"]);
    restored.AgentScopes.Select(scope => scope.Scope).ShouldBe(["identity:read"]);
    (await store.FindAsync(RoleId.From(strayScopeOnly))).ShouldNotBeNull().AgentScopes.Select(scope => scope.Scope).ShouldBe(["demo:invoke"]);
    (await store.ListAsync()).Count.ShouldBe(RoleIds.All.Count + 2);
  }
}
