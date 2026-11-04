#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu tests for InMemoryRoleStore and the two role grant read ports over it (task 473).
// Run standalone:  dotnet run source/container-apps/web/features/admin/roles/in-memory-role-store-tests.cs

#region Purpose
// Host-free coverage of the in-memory Role store: product-role seed, snapshot reads, write errors, delete guard, read ports.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Roles
{

  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Architecture.Features.Admin.Roles.Application;
  using TimeWarp.Architecture.Features.Admin.Roles.Domain;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class InMemoryRoleStore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InMemoryRoleStore_Given_>();

    public static async Task NewStore_Should_HoldTheProductRolesWithSeedGrants()
    {
      InMemoryRoleStore store = new();
      IReadOnlyList<Role> roles = await store.ListAsync();

      roles.Select(static role => role.Id.Value).ShouldBe(RoleIds.All, ignoreOrder: true);
      foreach (ProductRoleSeed.Entry seed in ProductRoleSeed.Roles)
      {
        Role role = roles.Single(role => role.Id.Value == seed.Id);
        role.Name.ShouldBe(seed.Name);
        role.Description.ShouldBe(seed.Description);
        role.Permissions.Select(static permission => permission.PermissionId)
          .ShouldBe(RolePermissionSeed.DefaultGrants[seed.Id].Distinct(StringComparer.Ordinal));
        role.AgentScopes.ShouldBeEmpty();
      }
    }

    public static async Task Reads_Should_BeSnapshotsUntilUpdate()
    {
      InMemoryRoleStore store = new();
      var memberId = RoleId.From(RoleIds.Member);
      Role first = (await store.FindAsync(memberId)).ShouldNotBeNull();
      first.UpdateDetails("Renamed", "Changed in memory only.");
      first.SetAgentScopes(["identity:read"]);

      Role unchanged = (await store.FindAsync(memberId)).ShouldNotBeNull();
      unchanged.Name.ShouldBe("Member");
      unchanged.AgentScopes.ShouldBeEmpty();

      await store.UpdateAsync(first);
      first.SetAgentScopes([]);
      Role saved = (await store.FindAsync(memberId)).ShouldNotBeNull();
      saved.Name.ShouldBe("Renamed");
      saved.AgentScopes.Select(static scope => scope.Scope).ShouldBe(["identity:read"]);
    }

    public static async Task Add_Should_RejectDuplicatesAndFindMany_Should_SkipMissing()
    {
      InMemoryRoleStore store = new();
      var role = Role.Create("Auditor", "Read-only access.");
      await store.AddAsync(role);

      await Should.ThrowAsync<InvalidOperationException>(() => store.AddAsync(Role.Create(role.Id, "Again", "Duplicate.")));
      (await store.FindManyAsync([role.Id, RoleId.New(), role.Id])).Select(static found => found.Id).ShouldBe([role.Id]);
      (await store.FindAsync(RoleId.New())).ShouldBeNull();
    }

    public static async Task UpdateAndRemove_Should_RejectUnknownIds()
    {
      InMemoryRoleStore store = new();
      await Should.ThrowAsync<InvalidOperationException>(() => store.UpdateAsync(Role.Create("Ghost", "Not stored.")));
      await Should.ThrowAsync<InvalidOperationException>(() => store.RemoveAsync(RoleId.New()));
    }

    public static async Task Remove_Should_DeleteCustomRolesAndRefuseProductRoles()
    {
      InMemoryRoleStore store = new();
      var role = Role.Create("Auditor", "Read-only access.");
      await store.AddAsync(role);

      await store.RemoveAsync(role.Id);
      (await store.FindAsync(role.Id)).ShouldBeNull();

      foreach (Guid productRoleId in RoleIds.All)
      {
        await Should.ThrowAsync<InvalidOperationException>(() => store.RemoveAsync(RoleId.From(productRoleId)));
        (await store.FindAsync(RoleId.From(productRoleId))).ShouldNotBeNull();
      }
    }

    public static async Task ReadPorts_Should_SeeAggregateWrites()
    {
      InMemoryRoleStore store = new();
      InMemoryRolePermissionStore permissions = new(store);
      InMemoryRoleAgentScopeStore scopes = new(store);

      Role developer = (await store.FindAsync(RoleId.From(RoleIds.Developer))).ShouldNotBeNull();
      developer.SetPermissions([PermissionIds.DeveloperAccess]);
      developer.SetAgentScopes(["feedback:read"]);
      await store.UpdateAsync(developer);

      (await permissions.GetPermissionIdsForRoleAsync(RoleIds.Developer)).ShouldBe([PermissionIds.DeveloperAccess]);
      (await permissions.GetPermissionIdsForRoleAsync(Guid.NewGuid())).ShouldBeEmpty();
      (await scopes.GetScopesForRoleAsync(RoleIds.Developer)).ShouldBe(["feedback:read"]);
      IReadOnlyDictionary<Guid, IReadOnlyList<string>> batch =
        await scopes.GetScopesForRolesAsync([RoleIds.Developer, RoleIds.Member, Guid.NewGuid()]);
      batch.Keys.ShouldBe([RoleIds.Developer]);
    }
  }

} // namespace TimeWarp.Architecture.Features.Admin.Roles
