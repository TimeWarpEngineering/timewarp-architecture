#region Purpose
// Registers the root route and CrossSliceReference for LoginPage; markup and behavior live in HomePage.razor.
#endregion

#region Design
// Public marketing / first-run entry (147-005). Anonymous by design for the route itself;
// AuthorizeView differentiates anonymous (soft Sign in → LoginPage) vs signed-in strip
// (ProfileState avatar/display name + Settings + Admin when PermissionIds.AdminAccess).
// Demo "Try it" actions live on Developer-gated TestPage — Home carries no demo residue.
// Ceremony ownership: only LoginPage runs passkey sign-in / create account. Home never
// clones that chrome (no "Sign in with a passkey" button that only redirects) — a single
// "Sign in" CTA navigates to /Login. FluentButton + NoSubRouteState.ChangeRoute (Profile
// pattern) — never nest a button inside NavLink (invalid HTML).
// Task 506 (TWA0050): Settings and Admin in the signed-in strip are RouteLinks, so each click
// is RouteState.ChangeRoute. The "Built with" NavLinks stay: each is an https link with
// target="_blank", which TWA0050 does not report.
// RFC 219 D8: Entra-without-passkey soft prompt is composed by TimeWarpPage (AddPasskeyPrompt),
// not this page. Home stays anonymous; the banner is not a route gate.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

// Public marketing / first-run entry. Anonymous by design.
[Page("/", Navigable = true, Description = "Public welcome page for TimeWarp.Architecture, with a sign-in entry.", NavOrder = 10)]
[CrossSliceReference(typeof(LoginPage), "First-run home CTA navigates to Identity login (focused passkey chrome).")]
partial class HomePage;
