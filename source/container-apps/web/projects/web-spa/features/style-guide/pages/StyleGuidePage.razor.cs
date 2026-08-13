#region Purpose
// Registers the StyleGuide route, authorize policy, and CrossSliceReference; markup and behavior live in StyleGuidePage.razor.
#endregion

#region Design
// The style guide is a living reference, so demos must exercise production code paths
// (state action sets, mediator behaviors) rather than shortcut service calls — what the
// page shows is exactly what real features get.
// The markup gallery and interactive handlers live in the .razor @code block.
#endregion

namespace TimeWarp.Architecture.Features.StyleGuide;

[Page("/StyleGuide", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
[CrossSliceReference(typeof(CounterState), "Living style guide deliberately exercises the counter throw-exception pipeline.")]
partial class StyleGuidePage;
