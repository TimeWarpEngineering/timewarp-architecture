#region Purpose
// Registers the route for a scratch page used for ad-hoc debugging experiments.
#endregion

namespace TimeWarp.Architecture.Features.Debugger;

[Page("/Debugger/Test", Policy = Policies.CanViewDeveloperPage)]
[Authorize(Policy = Policies.CanViewDeveloperPage)]
partial class TestPage {}
