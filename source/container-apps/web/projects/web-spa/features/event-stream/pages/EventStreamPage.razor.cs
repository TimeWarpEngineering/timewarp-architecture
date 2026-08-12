#region Purpose
// Registers the route for the page demonstrating the EventStream middleware's captured action log.
#endregion

namespace TimeWarp.Architecture.Features.EventStreams;

[Page("/EventStream", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class EventStreamPage;
