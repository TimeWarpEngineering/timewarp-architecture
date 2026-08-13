#region Purpose
// Registers the Passkeys route and authorize policy; markup and behavior live in PasskeysPage.razor.
#endregion

#region Design
// Product human CTA lives on /Login (task 104-016). This page remains a discoverable technical
// demo under Nav → Pages so operators can exercise the raw ceremony without the product copy.
// Ceremony mapping is shared via PasskeyCeremonyClient — do not reintroduce Passwordless.dev or
// direct passwordless.* JS interop here.
// Mock mode: ceremony contracts have no GetMockResponseFactory; mock chain yields 501 and we
// surface it through ErrorMessage.
// RP-ID credential scoping (task 104-031): register and authenticate on the SAME host.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

// Technical ceremony demo — product CTA is /Login. Nav + route gated to Developer (147-001).
[Page("/Passkeys", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class PasskeysPage;
