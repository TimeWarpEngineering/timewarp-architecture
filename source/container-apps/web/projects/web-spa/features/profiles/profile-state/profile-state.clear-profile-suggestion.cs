#region Purpose
// ClearProfileSuggestionActionSet: drops a profile draft after the page has applied it.
#endregion

#region Design
// Parameterless, so it would be a Ctrl-K command without the opt-out. It is not a catalog
// action. The page dispatches it after copying the supplied fields onto the working copy.
// Sign-out clears the same fields through Initialize.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

partial class ProfileState
{
  public static class ClearProfileSuggestionActionSet
  {
    [CommandPaletteOptOut("Clears a profile draft after the page applies it. The profile page runs it.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        ProfileState.ProfileSuggestionJson = null;
        ProfileState.ProfileSuggestionId = null;
        ProfileState.ProfileSuggestionAccepted = false;
        return default;
      }
    }
  }
}
