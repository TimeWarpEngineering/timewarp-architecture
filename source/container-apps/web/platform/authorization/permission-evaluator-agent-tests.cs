#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008
#:include permission-evaluator-fakes-tests.cs

// Co-located Jaribu tests for PermissionEvaluator agent-token scope expansion (182-001 / 182-006).
// Run standalone:  dotnet run source/container-apps/web/platform/authorization/permission-evaluator-agent-tests.cs

#region Purpose
// Host-free coverage of agent-token permission expansion: ambient caller, held scopes, unions, and never granting admin.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Authorization
{

  using TimeWarp.Architecture.Features;

  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Authorization.PermissionEvaluatorFactory;

  [TestTag("Application")]
  public class PermissionEvaluatorAgentToken_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<PermissionEvaluatorAgentToken_Given_>();

    public static async Task AgentToken_WithoutAmbientCaller_Should_YieldEmptyEvenWithAdminRoles()
    {
      InMemoryPrincipalRoleStore principalRoles = new();
      PrincipalId id = PrincipalId.New();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        RoleGrantFixture.SeededPermissions(),
        bootstrap: [],
        agentScopes: FakeAgentPermissionScopeSource.Empty);

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

    public static async Task AgentToken_WithScopes_Should_GrantMappedOnly()
    {
      PrincipalId id = PrincipalId.New();
      InMemoryPrincipalRoleStore principalRoles = new();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        RoleGrantFixture.SeededPermissions(),
        bootstrap: [],
        agentScopes: FakeAgentPermissionScopeSource.For(id, [AgentScopes.IdentityRead]));

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.AgentToken);

      permissions.ShouldBe(
      [
        PermissionIds.AgentLinkManageSelf,
        PermissionIds.IdentityRead,
      ]);
      (await evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.AgentToken, PermissionIds.IdentityRead))
        .ShouldBeTrue();
      (await evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.AgentToken, PermissionIds.AdminAccess))
        .ShouldBeFalse();
      (await evaluator.HasPermissionAsync(id, AuthenticationSchemeNames.AgentToken, PermissionIds.CredentialManageSelf))
        .ShouldBeFalse();
    }

    public static async Task AgentToken_MultipleScopes_Should_UnionPermissions()
    {
      PrincipalId id = PrincipalId.New();
      PermissionEvaluator evaluator = CreateEvaluator(
        new InMemoryPrincipalRoleStore(),
        RoleGrantFixture.SeededPermissions(),
        bootstrap: [],
        agentScopes: FakeAgentPermissionScopeSource.For(
          id,
          [AgentScopes.IdentityRead, AgentScopes.DemoInvoke, AgentScopes.CredentialManage]));

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.AgentToken);

      permissions.ShouldBe(
      [
        PermissionIds.AgentLinkManageSelf,
        PermissionIds.IdentityRead,
        PermissionIds.CredentialManageSelf,
        PermissionIds.DemoInvoke,
      ]);
    }

    public static async Task AgentToken_UnknownScope_Should_BeIgnored()
    {
      PrincipalId id = PrincipalId.New();
      PermissionEvaluator evaluator = CreateEvaluator(
        new InMemoryPrincipalRoleStore(),
        RoleGrantFixture.SeededPermissions(),
        bootstrap: [],
        agentScopes: FakeAgentPermissionScopeSource.For(
          id,
          ["not-a-real-scope", AgentScopes.DemoInvoke]));

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.AgentToken);

      permissions.ShouldBe([PermissionIds.DemoInvoke]);
    }

    public static async Task AgentToken_PrincipalMismatch_Should_YieldEmpty()
    {
      PrincipalId requested = PrincipalId.New();
      PrincipalId ambient = PrincipalId.New();
      PermissionEvaluator evaluator = CreateEvaluator(
        new InMemoryPrincipalRoleStore(),
        RoleGrantFixture.SeededPermissions(),
        bootstrap: [],
        agentScopes: FakeAgentPermissionScopeSource.For(
          ambient,
          [AgentScopes.IdentityRead]));

      (await evaluator.GetPermissionsAsync(requested, AuthenticationSchemeNames.AgentToken))
        .ShouldBeEmpty();
    }

    public static async Task AgentToken_NeverGrantsAdmin_EvenWithAdminRolesInStore()
    {
      PrincipalId id = PrincipalId.New();
      InMemoryPrincipalRoleStore principalRoles = new();
      await principalRoles.SetRoleIdsAsync(id, [RoleIds.Administrator]);

      PermissionEvaluator evaluator = CreateEvaluator(
        principalRoles,
        RoleGrantFixture.SeededPermissions(),
        bootstrap: [],
        agentScopes: FakeAgentPermissionScopeSource.For(
          id,
          [AgentScopes.IdentityRead, AgentScopes.CredentialManage, AgentScopes.DemoInvoke]));

      IReadOnlyList<string> permissions = await evaluator.GetPermissionsAsync(
        id,
        AuthenticationSchemeNames.AgentToken);

      foreach (string adminPermission in RolePermissionSeed.AdminPermissions)
      {
        permissions.ShouldNotContain(adminPermission);
      }

      permissions.ShouldBe(
      [
        PermissionIds.AgentLinkManageSelf,
        PermissionIds.IdentityRead,
        PermissionIds.CredentialManageSelf,
        PermissionIds.DemoInvoke,
      ]);
    }
  }
}
