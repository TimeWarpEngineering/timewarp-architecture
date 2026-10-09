#region Purpose
// Records the Identity edge for AgentAsk's sign-in button; markup and behavior live in AgentAsk.razor.
#endregion

#region Design
// Task 293: a 401 renders "Sign in to use Ask". The button closes the panel (task 292 docked it) and calls
// RouteState.ChangeRoute with LoginPage.GetLoginUrl for the current path. TWA0026 forbids
// NavigationManager.NavigateTo in a component. Razor @code is outside TWA0009, so the
// Applications → Identity edge is declared here, the same way HomePage declares its Sign in CTA.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[CrossSliceReference(typeof(LoginPage), "Ask sign-in button navigates to Identity login and returns to the current page.")]
partial class AgentAsk;
