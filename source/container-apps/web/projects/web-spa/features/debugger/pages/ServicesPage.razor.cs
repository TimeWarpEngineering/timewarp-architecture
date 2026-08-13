#region Purpose
// Registers the Services route and authorize policy; markup and behavior live in ServicesPage.razor.
#endregion

#region Design
// Pipeline ordering and duplicate registrations are invisible at runtime; this page
// exposes them by injecting IServiceCollection, which program.cs registers into
// itself as a singleton specifically to enable this introspection.
// Diagnostic-only — nothing here should be load-bearing for features.
#endregion

namespace TimeWarp.Architecture.Features.Debugger;

[Page("/Services", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class ServicesPage;
