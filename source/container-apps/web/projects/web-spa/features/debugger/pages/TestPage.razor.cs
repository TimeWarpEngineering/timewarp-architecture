#region Purpose
// Registers the route for a Developer-gated scratch page and demo action surface.
#endregion

#region Design
// Task 147-005: "Try it" (tracked task buttons + assembly-info modal) moved here from Home so
// the professional first-run surface carries no demo residue. Still gated by
// PermissionIds.DeveloperAccess (147-001 developer nav philosophy / 182-003).
#endregion

namespace TimeWarp.Architecture.Features.Debugger;

[Page("/Debugger/Test", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class TestPage;
