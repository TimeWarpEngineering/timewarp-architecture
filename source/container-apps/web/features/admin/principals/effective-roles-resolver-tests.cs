#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for EffectiveRolesResolver SSOT algorithm (147-004).

#region Purpose
// Host-free coverage of empty→Member, exact stored set, bootstrap union, invalid bootstrap ignore, ordering.
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
  public class EffectiveRolesResolver_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<EffectiveRolesResolver_Given_>();

    public static async Task EmptyStore_Should_YieldMemberOnly()
    {
      EffectiveRolesResolver resolver = CreateResolver(
        store: new InMemoryPrincipalRoleStore(),
        bootstrap: []);
      PrincipalId id = PrincipalId.New();

      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member]);
    }

    public static async Task NonEmptyStore_Should_UseExactlyStoredRoles()
    {
      InMemoryPrincipalRoleStore store = new();
      PrincipalId id = PrincipalId.New();
      await store.SetRoleIdsAsync(id, [RoleIds.Administrator, RoleIds.Developer]);

      EffectiveRolesResolver resolver = CreateResolver(store, bootstrap: []);
      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Administrator, RoleIds.Developer]);
      roles.ShouldNotContain(RoleIds.Member);
    }

    public static async Task Bootstrap_Should_UnionAdministratorAndMember()
    {
      PrincipalId id = PrincipalId.New();
      InMemoryPrincipalRoleStore store = new();
      await store.SetRoleIdsAsync(id, [RoleIds.Developer]);

      EffectiveRolesResolver resolver = CreateResolver(store, bootstrap: [id.Value.ToString()]);
      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member, RoleIds.Administrator, RoleIds.Developer]);
    }

    public static async Task BootstrapEmptyStore_Should_YieldMemberAndAdministrator()
    {
      PrincipalId id = PrincipalId.New();
      EffectiveRolesResolver resolver = CreateResolver(
        new InMemoryPrincipalRoleStore(),
        bootstrap: [id.Value.ToString()]);

      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member, RoleIds.Administrator]);
    }

    public static async Task InvalidBootstrapGuid_Should_BeIgnored()
    {
      PrincipalId id = PrincipalId.New();
      EffectiveRolesResolver resolver = CreateResolver(
        new InMemoryPrincipalRoleStore(),
        bootstrap: ["not-a-guid", Guid.Empty.ToString()]);

      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member]);
    }

    public static async Task Ordering_Should_FollowRoleIdsAll()
    {
      InMemoryPrincipalRoleStore store = new();
      PrincipalId id = PrincipalId.New();
      await store.SetRoleIdsAsync(
        id,
        [RoleIds.Developer, RoleIds.Member, RoleIds.Operator, RoleIds.Administrator]);

      EffectiveRolesResolver resolver = CreateResolver(store, bootstrap: []);
      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe(RoleIds.All);
    }

    private static EffectiveRolesResolver CreateResolver(
      IPrincipalRoleStore store,
      string[] bootstrap)
    {
      IOptions<BootstrapAdministratorOptions> options = Options.Create(
        new BootstrapAdministratorOptions
        {
          BootstrapAdministratorPrincipalIds = bootstrap
        });
      return new EffectiveRolesResolver(store, options);
    }
  }
}
