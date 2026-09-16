#region Purpose
// Settings Microsoft 365 section is gated on GetEntraSignInOffered, same server flag as Login.
#endregion

#region Design
// HTML proof lives with the prerender host (protected-page-deep-link-tests). This file pins the
// contract the Settings page calls so a client-side Authentication:Entra:Enabled key cannot
// sneak back in as the gate.
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
}
