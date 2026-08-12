#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for PermissionEvaluator + InMemoryRolePermissionStore seed (182-001).

#region Purpose
// Host-free coverage of role→permission expansion, scheme gates, and admin read/manage split seed.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features
{

  using System.Threading.Tasks;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class PermissionEvaluator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<PermissionEvaluator_Given_>();

    public static async Task EmptyPrincipalStore_Should_ExpandMemberSelfService()
    {
      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles: new InMemoryPrincipalRoleStore(),
        rolePermissions: new InMemoryRolePermissionStore(),
        bootstrap: []);
      PrincipalId id = PrincipalId.New();

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.IdentitySession);

      permissions.ShouldBe(
      [
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead,
      ]);
      (await evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.IdentitySession, PermissionIds.ProfileRead))
        .ShouldBeTrue();
      (await evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.IdentitySession, PermissionIds.AdminAccess))
        .ShouldBeFalse();
    }

    public static async Task Administrator_Should_ExpandAdminAndSelfService()
    {
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        new InMemoryRolePermissionStore(),
        bootstrap: []);

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.IdentitySession);

      permissions.ShouldContain(PermissionIds.AdminAccess);
      permissions.ShouldContain(PermissionIds.AdminRolesRead);
      permissions.ShouldContain(PermissionIds.AdminRolesManage);
      permissions.ShouldContain(PermissionIds.AdminPrincipalsRead);
      permissions.ShouldContain(PermissionIds.AdminPrincipalsManage);
      permissions.ShouldContain(PermissionIds.ProfileRead);
      permissions.ShouldContain(PermissionIds.SettingsRead);
      permissions.ShouldNotContain(PermissionIds.DeveloperAccess);
    }

    public static async Task Member_Should_NotHaveAdminPermissions()
    {
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Member]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        new InMemoryRolePermissionStore(),
        bootstrap: []);

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.MockIdentitySession);

      permissions.ShouldBe(
      [
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead,
      ]);
      foreach (string adminPermission in RolePermissionSeed.AdminPermissions)
      {
        permissions.ShouldNotContain(adminPermission);
        (await evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.MockIdentitySession, adminPermission))
          .ShouldBeFalse();
      }
    }

    public static async Task ReadManage_SeedSets_Should_DifferAndBeSeparable()
    {
      // Registry ids are distinct (1:1 policy names); seed Administrator carries both halves.
      PermissionIds.AdminRolesRead.ShouldNotBe(PermissionIds.AdminRolesManage);
      PermissionIds.AdminPrincipalsRead.ShouldNotBe(PermissionIds.AdminPrincipalsManage);
      RolePermissionSeed.DefaultGrants[RoleIds.Administrator]
        .ShouldContain(PermissionIds.AdminRolesRead);
      RolePermissionSeed.DefaultGrants[RoleIds.Administrator]
        .ShouldContain(PermissionIds.AdminRolesManage);

      // Store can grant read without manage (rebundle without a new product role Guid).
      // EffectiveRolesResolver only emits RoleIds.All entries, so rebundle Administrator grants.
      InMemoryRolePermissionStore rolePermissions = new();
      await rolePermissions.SetPermissionIdsForRoleAsync(
        RoleIds.Administrator,
        [
          PermissionIds.AdminAccess,
          PermissionIds.AdminRolesRead,
          PermissionIds.AdminPrincipalsRead,
          PermissionIds.ProfileRead,
          PermissionIds.SettingsRead,
        ]);

      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        rolePermissions,
        bootstrap: []);

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.IdentitySession);

      permissions.ShouldContain(PermissionIds.AdminRolesRead);
      permissions.ShouldContain(PermissionIds.AdminPrincipalsRead);
      permissions.ShouldNotContain(PermissionIds.AdminRolesManage);
      permissions.ShouldNotContain(PermissionIds.AdminPrincipalsManage);
    }

    public static async Task AgentTokenScheme_Should_YieldEmptyPermissions()
    {
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        new InMemoryRolePermissionStore(),
        bootstrap: []);

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.AgentToken);

      permissions.ShouldBeEmpty();
      (await evaluator.HasPermissionAsync(
          id,
          AuthenticationSchemeNames.AgentToken,
          PermissionIds.AdminAccess))
        .ShouldBeFalse();
    }

    public static async Task NullOrUnknownScheme_Should_YieldEmptyPermissions()
    {
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        new InMemoryRolePermissionStore(),
        bootstrap: []);

      (await evaluator.GetPermissionsAsync(id, authenticationScheme: null)).ShouldBeEmpty();
      (await evaluator.GetPermissionsAsync(id, "some-other-scheme")).ShouldBeEmpty();
    }

    public static async Task BootstrapAdministrator_Should_UnionAdminPermissions()
    {
      PrincipalId id = PrincipalId.New();
      PermissionEvaluator evaluator = CreateEvaluator(
        new InMemoryPrincipalRoleStore(),
        new InMemoryRolePermissionStore(),
        bootstrap: [id.Value.ToString()]);

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.IdentitySession);

      permissions.ShouldContain(PermissionIds.AdminAccess);
      permissions.ShouldContain(PermissionIds.AdminRolesManage);
      permissions.ShouldContain(PermissionIds.ProfileRead);
    }

    public static async Task Developer_Should_ExpandDeveloperSet()
    {
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Developer]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        new InMemoryRolePermissionStore(),
        bootstrap: []);

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.IdentitySession);

      permissions.ShouldBe(
      [
        PermissionIds.DeveloperAccess,
        PermissionIds.DeveloperClaimsRead,
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead,
      ]);
      permissions.ShouldNotContain(PermissionIds.AdminAccess);
    }

    public static async Task PermissionPolicyRegistration_Should_ExposeAllPermissionIds()
    {
      await Task.CompletedTask;
      PermissionPolicyRegistration.AllPermissionPolicyNames.ShouldBe(PermissionIds.All);
    }

    public static async Task ConcurrentChecks_Should_SingleFlightStoreExpansion()
    {
      // Task 183: Blazor SSR evaluates several policies concurrently in one request scope; the
      // scoped evaluator must share ONE sequential store expansion or concurrent queries race the
      // scoped PostgresDbContext ("A second operation was started on this context" → page 500).
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      CountingRolePermissionStore countingStore = new(new InMemoryRolePermissionStore());
      PermissionEvaluator evaluator = CreateEvaluator(principalRoles, countingStore, bootstrap: []);

      Task<bool>[] checks =
      [
        evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.IdentitySession, PermissionIds.AdminRolesRead),
        evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.IdentitySession, PermissionIds.AdminAccess),
        evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.IdentitySession, PermissionIds.SettingsRead),
        evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.IdentitySession, PermissionIds.ProfileRead),
      ];
      bool[] results = await Task.WhenAll(checks);

      results.ShouldAllBe(static granted => granted);
      countingStore.MaxConcurrentGets.ShouldBe(1,
        "Concurrent permission checks must not overlap store queries (scoped DbContext is single-flight only).");
      countingStore.TotalGets.ShouldBe(1,
        "All concurrent checks for one principal must share a single expansion.");
    }

    private sealed class CountingRolePermissionStore : IRolePermissionStore
    {
      private readonly InMemoryRolePermissionStore Inner;
      private int InFlight;

      public CountingRolePermissionStore(InMemoryRolePermissionStore inner) => Inner = inner;

      public int TotalGets;
      public int MaxConcurrentGets;

      public async Task<IReadOnlyList<string>> GetPermissionIdsForRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
      {
        Interlocked.Increment(ref TotalGets);
        int concurrent = Interlocked.Increment(ref InFlight);
        InterlockedExtensions.Max(ref MaxConcurrentGets, concurrent);
        try
        {
          await Task.Delay(25, cancellationToken); // widen the race window
          return await Inner.GetPermissionIdsForRoleAsync(roleId, cancellationToken);
        }
        finally
        {
          Interlocked.Decrement(ref InFlight);
        }
      }

      public Task SetPermissionIdsForRoleAsync(
        Guid roleId,
        IReadOnlyList<string> permissionIds,
        CancellationToken cancellationToken = default) =>
        Inner.SetPermissionIdsForRoleAsync(roleId, permissionIds, cancellationToken);
    }

    private static class InterlockedExtensions
    {
      public static void Max(ref int location, int value)
      {
        int current;
        while ((current = Volatile.Read(ref location)) < value
          && Interlocked.CompareExchange(ref location, value, current) != current)
        {
        }
      }
    }

    private static PermissionEvaluator CreateEvaluator(
      IPrincipalRoleStore principalRoles,
      IRolePermissionStore rolePermissions,
      string[] bootstrap)
    {
      IOptions<BootstrapAdministratorOptions> options = Options.Create(
        new BootstrapAdministratorOptions
        {
          BootstrapAdministratorPrincipalIds = bootstrap
        });
      EffectiveRolesResolver resolver = new(principalRoles, options);
      return new PermissionEvaluator(resolver, rolePermissions);
    }
  }

  [TestTag("Application")]
  public class InMemoryRolePermissionStore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InMemoryRolePermissionStore_Given_>();

    public static async Task Constructor_Should_SeedDefaultGrants()
    {
      InMemoryRolePermissionStore store = new();

      foreach ((Guid roleId, IReadOnlyList<string> expected) in RolePermissionSeed.DefaultGrants)
      {
        IReadOnlyList<string> actual = await store.GetPermissionIdsForRoleAsync(roleId);
        actual.ShouldBe(expected, ignoreOrder: true);
      }
    }

    public static async Task Set_Should_ReplaceAndClear()
    {
      InMemoryRolePermissionStore store = new();
      Guid roleId = RoleIds.Operator;

      await store.SetPermissionIdsForRoleAsync(roleId, [PermissionIds.AdminAccess]);
      (await store.GetPermissionIdsForRoleAsync(roleId)).ShouldBe([PermissionIds.AdminAccess]);

      await store.SetPermissionIdsForRoleAsync(roleId, []);
      (await store.GetPermissionIdsForRoleAsync(roleId)).ShouldBeEmpty();
    }
  }
}
