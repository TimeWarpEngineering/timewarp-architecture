#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.DependencyInjection
#:package Microsoft.AspNetCore.Authorization
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for SPA permission claim policy registration (182-003).

#region Purpose
// Host-free coverage that AddPermissionClaimPolicies registers every PermissionIds entry and
// IAuthorizationService succeeds only when the principal carries the matching claim.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features
{

  using System.Security.Claims;
  using System.Threading.Tasks;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Logging.Abstractions;
  using Shouldly;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class PermissionClaimPolicies_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<PermissionClaimPolicies_Given_>();

    public static Task Registry_Should_Register_Every_PermissionId()
    {
      AuthorizationOptions options = new();
      PermissionPolicyRegistration.AddPermissionClaimPolicies(options);

      foreach (string permissionId in PermissionIds.All)
      {
        options.GetPolicy(permissionId).ShouldNotBeNull(
          $"Expected SPA claim policy for '{permissionId}'.");
      }

      PermissionPolicyRegistration.AllPermissionPolicyNames.ShouldBe(PermissionIds.All);
      return Task.CompletedTask;
    }

    public static Task Server_PermissionRequirement_Policies_Should_Not_Be_Claim_Based()
    {
      AuthorizationOptions options = new();
      PermissionPolicyRegistration.AddPermissionPolicies(options);

      AuthorizationPolicy? policy = options.GetPolicy(PermissionIds.AdminRolesRead);
      policy.ShouldNotBeNull();
      policy.Requirements.ShouldContain(r => r is PermissionRequirement);
      policy.Requirements.ShouldNotContain(r => r.GetType().Name.Contains("Claims", StringComparison.Ordinal));
      return Task.CompletedTask;
    }

    public static async Task Principal_With_Permission_Claim_Should_Succeed()
    {
      IAuthorizationService authz = CreateClaimAuthorizationService();
      ClaimsPrincipal user = PrincipalWithPermissions(PermissionIds.AdminRolesRead);

      AuthorizationResult result = await authz.AuthorizeAsync(user, resource: null, PermissionIds.AdminRolesRead);

      result.Succeeded.ShouldBeTrue();
    }

    public static async Task Principal_Without_Permission_Claim_Should_Fail()
    {
      IAuthorizationService authz = CreateClaimAuthorizationService();
      ClaimsPrincipal user = PrincipalWithPermissions(PermissionIds.ProfileRead);

      AuthorizationResult result = await authz.AuthorizeAsync(user, resource: null, PermissionIds.AdminRolesRead);

      result.Succeeded.ShouldBeFalse();
    }

    public static async Task SelfService_Composition_Should_Not_Grant_Admin()
    {
      IAuthorizationService authz = CreateClaimAuthorizationService();
      ClaimsPrincipal member = PrincipalWithPermissions(
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead);

      (await authz.AuthorizeAsync(member, resource: null, PermissionIds.ProfileRead))
        .Succeeded.ShouldBeTrue();
      (await authz.AuthorizeAsync(member, resource: null, PermissionIds.SettingsRead))
        .Succeeded.ShouldBeTrue();
      (await authz.AuthorizeAsync(member, resource: null, PermissionIds.AdminAccess))
        .Succeeded.ShouldBeFalse();
      (await authz.AuthorizeAsync(member, resource: null, PermissionIds.DeveloperAccess))
        .Succeeded.ShouldBeFalse();
    }

    public static async Task ClaimType_Must_Match_PermissionIds_ClaimType()
    {
      IAuthorizationService authz = CreateClaimAuthorizationService();
      // Wrong claim type, right value — must not authorize.
      ClaimsIdentity identity = new("test");
      identity.AddClaim(new Claim(ClaimTypes.Role, PermissionIds.AdminAccess));
      ClaimsPrincipal user = new(identity);

      AuthorizationResult result = await authz.AuthorizeAsync(user, resource: null, PermissionIds.AdminAccess);

      result.Succeeded.ShouldBeFalse();
    }

    private static IAuthorizationService CreateClaimAuthorizationService()
    {
      ServiceCollection services = new();
      // DefaultAuthorizationService requires ILogger<>; NullLoggerFactory avoids a Logging package pin.
      services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
      services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
      services.AddAuthorizationCore(PermissionPolicyRegistration.AddPermissionClaimPolicies);
      return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal PrincipalWithPermissions(params string[] permissionIds)
    {
      ClaimsIdentity identity = new("test");
      foreach (string permissionId in permissionIds)
      {
        identity.AddClaim(new Claim(PermissionIds.ClaimType, permissionId));
      }

      return new ClaimsPrincipal(identity);
    }
  }
}
