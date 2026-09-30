#region Purpose
// Registers the Counter route and authorize policy; markup and behavior live in CounterPage.razor.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

[Page("/Counter", Policy = PermissionIds.DeveloperAccess, Navigable = true)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class CounterPage;
