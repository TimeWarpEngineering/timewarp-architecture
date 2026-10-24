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
// The Confirmations card only dispatches StyleGuideState.ConfirmSample. The handler asks
// through IConfirmationService, the same service ApiHandler.ConfirmAsync overrides call.
// StyleGuidePage_Should_ is the page contract for the upstreamed catalog. The form fields are
// the only stored choice. The sample drawer follows ApplicationState.
#endregion

namespace TimeWarp.Architecture.Features.StyleGuide;

[Page("/StyleGuide", Policy = PermissionIds.DeveloperAccess, Navigable = true, Description = "Visual style guide for template components.", NavGroup = "Developer", NavOrder = 310)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
[CrossSliceReference(typeof(CounterState), "Living style guide deliberately exercises the counter throw-exception pipeline.")]
[CrossSliceReference(typeof(ProfilePage), "Style guide sample links the profile page from the user card.")]
[PageLocalMessageBar("Style guide showcases the FluentMessageBar component itself; these bars are documentation, not operation outcomes.")]
partial class StyleGuidePage;
