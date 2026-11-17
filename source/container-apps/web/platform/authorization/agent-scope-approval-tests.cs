#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu test for AgentScopeApproval source marking (task 466).

#region Purpose
// Host-free coverage of which source marks each approved agent scope, and the catalog order.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Authorization
{

  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Architecture.Features.Admin.Roles.Application;
  using TimeWarp.Architecture.Features.Admin.Roles.Domain;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class AgentScopeApproval_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<AgentScopeApproval_Given_>();

    public static async Task MixedSources_Should_MarkDefaultRoleAndPrincipalInCatalogOrder()
    {
      // Role scopes are written through the Role aggregate; the port is a view over that store.
      InMemoryRoleStore roles = new();
      await SetScopesAsync(roles, RoleIds.Member, [AgentScopes.FeedbackRead]);
      await SetScopesAsync(
        roles,
        RoleIds.Developer,
        [AgentScopes.FeedbackRead, AgentScopes.IdentityRead, AgentScopes.DemoInvoke]);
      await SetScopesAsync(roles, RoleIds.Operator, [AgentScopes.FeedbackNote]);
      InMemoryRoleAgentScopeStore roleScopes = new(roles);

      InMemoryPrincipalAgentScopeStore principalScopes = new();
      PrincipalId principalId = PrincipalId.New();
      await principalScopes.SetScopesAsync(
        principalId,
        [
          "not-a-scope",
          AgentScopes.CredentialManage,
          AgentScopes.FeedbackRead,
          AgentScopes.IdentityRead,
        ]);

      AgentScopeApproval approval = new(
        new FixedRoles([RoleIds.Member, RoleIds.Developer]),
        roleScopes,
        principalScopes);

      AgentScopeApprovalSnapshot snapshot = await approval.GetSnapshotAsync(principalId);

      snapshot.PrincipalGrants.ShouldBe(
      [
        AgentScopes.IdentityRead,
        AgentScopes.CredentialManage,
        AgentScopes.FeedbackRead,
      ]);

      snapshot.Effective.Select(static item => item.Scope).ShouldBe(
      [
        AgentScopes.IdentityRead,
        AgentScopes.DemoInvoke,
        AgentScopes.CredentialManage,
        AgentScopes.FeedbackRead,
      ]);

      ApprovedAgentScope identity = snapshot.Effective[0];
      identity.Sources.Count.ShouldBe(3);
      identity.Sources[0].Kind.ShouldBe(AgentScopeGrantKind.Default);
      identity.Sources[0].RoleId.ShouldBeNull();
      identity.Sources[1].Kind.ShouldBe(AgentScopeGrantKind.Role);
      identity.Sources[1].RoleId.ShouldBe(RoleIds.Developer);
      identity.Sources[2].Kind.ShouldBe(AgentScopeGrantKind.Principal);
      identity.Sources[2].RoleId.ShouldBeNull();

      ApprovedAgentScope demo = snapshot.Effective[1];
      demo.Sources.Count.ShouldBe(1);
      demo.Sources[0].Kind.ShouldBe(AgentScopeGrantKind.Role);
      demo.Sources[0].RoleId.ShouldBe(RoleIds.Developer);

      ApprovedAgentScope credential = snapshot.Effective[2];
      credential.Sources.Count.ShouldBe(1);
      credential.Sources[0].Kind.ShouldBe(AgentScopeGrantKind.Principal);
      credential.Sources[0].RoleId.ShouldBeNull();

      ApprovedAgentScope feedback = snapshot.Effective[3];
      feedback.Sources.Count.ShouldBe(3);
      feedback.Sources[0].Kind.ShouldBe(AgentScopeGrantKind.Role);
      feedback.Sources[0].RoleId.ShouldBe(RoleIds.Member);
      feedback.Sources[1].Kind.ShouldBe(AgentScopeGrantKind.Role);
      feedback.Sources[1].RoleId.ShouldBe(RoleIds.Developer);
      feedback.Sources[2].Kind.ShouldBe(AgentScopeGrantKind.Principal);
      feedback.Sources[2].RoleId.ShouldBeNull();
    }

    private static async Task SetScopesAsync(InMemoryRoleStore roles, Guid roleId, IReadOnlyList<string> scopes)
    {
      Role role = (await roles.FindAsync(RoleId.From(roleId))).ShouldNotBeNull();
      role.SetAgentScopes(scopes);
      await roles.UpdateAsync(role);
    }

    private sealed class FixedRoles : IEffectiveRolesResolver
    {
      private readonly IReadOnlyList<Guid> EffectiveRoleIds;

      public FixedRoles(IReadOnlyList<Guid> effectiveRoleIds)
      {
        EffectiveRoleIds = effectiveRoleIds;
      }

      public Task<IReadOnlyList<Guid>> GetEffectiveRoleIdsAsync(
        PrincipalId principalId,
        CancellationToken cancellationToken = default)
      {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(EffectiveRoleIds);
      }
    }
  }

}
