#region Purpose
// Registers the root route for the app's landing page; markup lives in HomePage.razor.
#endregion

#region Design
// Public marketing / first-run entry (147-005). Anonymous by design for the route itself;
// AuthorizeView differentiates anonymous (passkey CTA → LoginPage) vs signed-in strip
// (ProfileState avatar/alias + Settings + Admin when CanViewAdminSidebarNavSection).
// Demo "Try it" actions live on Developer-gated TestPage — Home carries no demo residue.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

// Public marketing / first-run entry. Anonymous by design.
[Page("/")]
partial class HomePage;
