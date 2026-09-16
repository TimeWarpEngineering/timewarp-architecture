#region Purpose
// Settings Microsoft 365 section is gated on GetEntraSignInOffered, same server flag as Login.
// Task 229: Link/Unlink predicates match the one-account and last-credential rules.
#endregion

#region Design
// HTML proof lives with the prerender host (protected-page-deep-link-tests). This file pins the
// contract the Settings page calls so a client-side Authentication:Entra:Enabled key cannot
// sneak back in as the gate, plus the host-free CanLink/CanUnlink formulas that prerender HTML
// and the server LastCredential 409 backstop must agree with.
#endregion

namespace SettingsPageMicrosoft365_;

using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Identity;

[TestTag("Unit")]
public class SettingsPage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SettingsPage_Should_>();

  public static Task Gate_On_The_Same_Offered_Query_As_Login()
  {
    SettingsPage.GetPageUrl().ShouldBe("/Settings");
    new GetEntraSignInOffered.Query().GetRoute().ShouldBe("api/identity/entra/offered");
    return Task.CompletedTask;
  }

  public static Task Link_Hidden_When_Already_Linked()
  {
    CredentialsState.CanLinkMicrosoft365(offered: true, activeEntraAccountCount: 0).ShouldBeTrue();
    CredentialsState.CanLinkMicrosoft365(offered: true, activeEntraAccountCount: 1).ShouldBeFalse();
    CredentialsState.CanLinkMicrosoft365(offered: false, activeEntraAccountCount: 0).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Unlink_Disabled_When_Last_Active_Agrees_With_Server_LastCredential()
  {
    CredentialsState.CanUnlink(0).ShouldBeFalse();
    CredentialsState.CanUnlink(1).ShouldBeFalse();
    CredentialsState.CanUnlink(2).ShouldBeTrue();
    TimeWarp.Architecture.Features.Identity.Application.IdentityProblems.LastCredential().Status.ShouldBe(409);
    TimeWarp.Architecture.Features.Identity.Application.IdentityProblems.LastCredential().Title.ShouldBe("Cannot revoke last credential");
    return Task.CompletedTask;
  }
}
