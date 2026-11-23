#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false

#region Purpose
// Proves generated From and Clone on the living profile, role, and site-settings contracts.
#endregion

#region Design
// This runfile sits outside the feature tree so the filename grammar does not claim it.
// It references web-contracts, where the details generator actually runs.
// Main is the entry point so the file can use a file-scoped namespace. Usings stay inside
// that namespace, which is where the global-usings analyzer allows them.
#endregion

namespace DetailsCopyContractsTests;

using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Identity;
using static TimeWarp.Jaribu.TestRunner;

public class DetailsCopyContractsTests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DetailsCopyContractsTests>();

#if !JARIBU_MULTI
  private static Task<int> Main() => RunAllTests();
#endif

  public static Task GetProfileCloneKeepsAvatarAndDoesNotShareTheInstance()
  {
    var response = new GetProfile.Response(
      displayName: "Ada",
      email: "ada@example.com",
      language: "en-US",
      region: "US",
      theme: "system",
      notifications: true,
      avatar: "data:image/svg");

    var clone = (GetProfile.Response)((System.ICloneable)response).Clone();
    clone.Avatar.ShouldBe("data:image/svg");
    clone.DisplayName.ShouldBe("Ada");
    clone.Email.ShouldBe("ada@example.com");
    clone.DisplayName = "Bea";
    response.DisplayName.ShouldBe("Ada");

    var command = UpdateProfile.Command.From(response);
    command.DisplayName.ShouldBe("Ada");
    command.Language.ShouldBe("en-US");
    command.Notifications.ShouldBeTrue();
    command.ShouldNotBeSameAs(response);
    return Task.CompletedTask;
  }

  public static Task GetRoleCloneKeepsRoleIdAndCopiesLists()
  {
    var roleId = System.Guid.Parse("11111111-1111-1111-1111-111111111111");
    List<string> permissions = ["read"];
    List<string> scopes = ["scope"];
    var response = new GetRole.Response(roleId, "Admin", "desc", permissions, scopes);

    var clone = (GetRole.Response)((System.ICloneable)response).Clone();
    clone.RoleId.ShouldBe(roleId);
    clone.Name.ShouldBe("Admin");
    clone.Permissions.ShouldNotBeSameAs(permissions);
    clone.AgentScopes.ShouldNotBeSameAs(scopes);
    clone.Permissions.ShouldBe(["read"]);
    permissions.Add("write");
    clone.Permissions.Count.ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task SiteSettingsFromCopiesVersionAndTenants()
  {
    var tenantId = System.Guid.Parse("22222222-2222-2222-2222-222222222222");
    List<EntraAcceptedTenant> tenants = [new EntraAcceptedTenant(tenantId, "example.com")];
    var loaded = new GetSiteSettings.Response(
      entraSignInEnabled: true,
      entraAllowBootstrap: false,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: "Ada",
      version: 7,
      entraAcceptedTenants: tenants);

    var clone = (GetSiteSettings.Response)((System.ICloneable)loaded).Clone();
    clone.Version.ShouldBe(7);
    clone.AssistantName.ShouldBe("Ada");
    clone.EntraAcceptedTenants.ShouldNotBeSameAs(loaded.EntraAcceptedTenants);
    clone.EntraAcceptedTenants.Count.ShouldBe(1);
    clone.EntraAcceptedTenants[0].ShouldBe(tenants[0]);

    var command = UpdateSiteSettings.Command.From(loaded);
    command.Version.ShouldBe(7);
    command.EntraSignInEnabled.ShouldBeTrue();
    command.EntraAcceptedTenants.ShouldNotBeSameAs(loaded.EntraAcceptedTenants);
    command.EntraAcceptedTenants[0].TenantId.ShouldBe(tenantId);

    var saved = new UpdateSiteSettings.Response(
      entraSignInEnabled: false,
      entraAllowBootstrap: true,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: "Ada",
      version: 8,
      entraAcceptedTenants: tenants);
    var again = UpdateSiteSettings.Command.From(saved);
    again.Version.ShouldBe(8);
    again.EntraAllowBootstrap.ShouldBeTrue();
    return Task.CompletedTask;
  }
}
