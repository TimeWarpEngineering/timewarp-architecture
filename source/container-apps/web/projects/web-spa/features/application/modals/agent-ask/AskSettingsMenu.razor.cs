#region Purpose
// Records the AgentLinks edge for AskSettingsMenu's Configure permissions item; markup and behavior live in AskSettingsMenu.razor.
#endregion

#region Design
// Task 356: Configure permissions closes the panel and dispatches RouteState.ChangeRoute with
// AgentLinksPage.GetPageUrl. Razor @code is outside TWA0009, so the Applications → AgentLinks edge
// is declared here, the same way AgentAsk declares its Sign in edge.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[CrossSliceReference(typeof(TimeWarp.Architecture.Features.AgentLinks.AgentLinksPage), "Ask settings links Configure permissions to the person's Agent links page.")]
partial class AskSettingsMenu;
