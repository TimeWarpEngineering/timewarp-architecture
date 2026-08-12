#region Purpose
// Registers the root route for the app's landing page; markup lives in HomePage.razor.
#endregion

#region Design
// Public marketing / first-run entry (147-005). Anonymous by design for the route itself;
// AuthorizeView differentiates anonymous (soft Sign in → LoginPage) vs signed-in strip
// (ProfileState avatar/alias + Settings + Admin when PermissionIds.AdminAccess).
// Demo "Try it" actions live on Developer-gated TestPage — Home carries no demo residue.
// Ceremony ownership: only LoginPage runs passkey sign-in / create account. Home never
// clones that chrome (no "Sign in with a passkey" button that only redirects) — a single
// "Sign in" CTA navigates to /Login. FluentButton + NoSubRouteState.ChangeRoute (Profile
// pattern) — never nest a button inside NavLink (invalid HTML).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

// Public marketing / first-run entry. Anonymous by design.
[Page("/")]
[CrossSliceReference(typeof(LoginPage), "First-run home CTA navigates to Account login (focused passkey chrome).")]
partial class HomePage
{
  private async Task GoToLoginAsync() =>
    await NoSubRouteState.ChangeRoute(newRoute: LoginPage.GetPageUrl());
}
