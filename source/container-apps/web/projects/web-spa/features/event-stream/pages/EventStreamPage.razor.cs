#region Purpose
// Registers the route for the page demonstrating the EventStream middleware's captured action log.
#endregion

namespace TimeWarp.Architecture.Features.EventStreams;

[Page("/EventStream", Policy = Policies.CanViewDeveloperPage)]
[Authorize(Policy = Policies.CanViewDeveloperPage)]
partial class EventStreamPage;
