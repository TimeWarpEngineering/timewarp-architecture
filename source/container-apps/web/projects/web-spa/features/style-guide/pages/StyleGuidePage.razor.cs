#region Purpose
// Registers the StyleGuide route, authorize policy, and CrossSliceReference; markup and behavior live in StyleGuidePage.razor.
#endregion

#region Design
// The style guide is a living reference, so demos must exercise production code paths
// (state action sets, mediator behaviors) rather than shortcut service calls — what the
// page shows is exactly what real features get.
// The markup gallery and interactive handlers live in the .razor @code block.
// The "Message bars (FluentUI)" card renders Success/Error FluentMessageBar as component
// documentation, not as operation outcomes — the reasoned PageLocalMessageBar opt-out below is
// the one TWA0025 exception in the SPA (task 247).
#endregion

namespace TimeWarp.Architecture.Features.StyleGuide;

[Page("/StyleGuide", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
[CrossSliceReference(typeof(CounterState), "Living style guide deliberately exercises the counter throw-exception pipeline.")]
[PageLocalMessageBar("Style guide showcases the FluentMessageBar component itself; these bars are documentation, not operation outcomes.")]
partial class StyleGuidePage;
