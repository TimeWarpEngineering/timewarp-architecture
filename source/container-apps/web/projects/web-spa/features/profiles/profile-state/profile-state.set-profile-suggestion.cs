#region Purpose
// SetProfileSuggestionActionSet: stores an agent draft of the profile form. Does not save.
#endregion

#region Design
// CatalogAction so the prefill tool can Execute it. Calling ISender.Send from the agent
// pipeline is TWA0022, including inside a helper. Visibility is Agent, and the action takes a
// required argument, so Ctrl-K does not list it. CommandPaletteOptOut keeps it out if the
// argument ever goes away. The handler writes the JSON, a new id, and clears Accepted. It does
// not send UpdateProfile, so there is no endpoint permission to mirror.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

partial class ProfileState
{
  public static class SetProfileSuggestionActionSet
  {
    [CatalogAction
    (
      Description = "Store an agent draft of the profile form without saving it.",
      Permissions = [PermissionIds.ProfileWrite],
      Visibility = ActionVisibility.Agent
    )]
    [CommandPaletteOptOut("Agent draft storage. The Prefill tool runs it; it is not a command.")]
    public sealed class Action : IBaseAction
    {
      public string SuggestionJson { get; }

      public Action(string suggestionJson)
      {
        ArgumentNullException.ThrowIfNull(suggestionJson);
        SuggestionJson = suggestionJson;
      }
    }

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        ProfileState.ProfileSuggestionJson = action.SuggestionJson;
        ProfileState.ProfileSuggestionId = Guid.NewGuid().ToString("N");
        ProfileState.ProfileSuggestionAccepted = false;
        return default;
      }
    }
  }
}
