#region Purpose
// Labels the derived Ctrl-K Ask row "Ask {AssistantName}" from AgentSurfaceState.
#endregion

#region Design
// Task 361 feeds task 301's runtime display override: the row stays the derived
// AgentSurface.OpenAskPanel command (same target, visibility and permissions); only its label and
// description change. The name is read when the palette opens, so a rename shows on the next open.
// ActionDisplay carries text only, so the row keeps the palette's default icon, not the avatar.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Gives the Ctrl-K Ask row the assistant's configured name.</summary>
public sealed class AskAssistantDisplayOverride : IActionDisplayOverride
{
  private readonly IStore Store;

  public AskAssistantDisplayOverride(IStore store)
  {
    Store = store;
  }

  /// <inheritdoc />
  public ActionDisplay? Find(ActionRosterEntry action)
  {
    ArgumentNullException.ThrowIfNull(action);
    if (action.ActionType != typeof(AgentSurfaceState.OpenAskPanelActionSet.Action))
    {
      return null;
    }

    AgentSurfaceState surface = Store.GetState<AgentSurfaceState>();
    return new ActionDisplay(surface.AskLabel, $"Open the {surface.AssistantName} panel to ask about this page or tell the app what to do.");
  }
}
