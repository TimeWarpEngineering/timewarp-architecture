#region Purpose
// ConfirmProfileSuggestionActionSet: asks before a draft replaces unsaved profile edits.
#endregion

#region Design
// The dialog stays in the handler (TWA0026). The page dispatches this only when its working
// copy is dirty. The message says the draft replaces unsaved changes on the form: the copy
// may differ because of an earlier draft, not only because the person typed. Decline clears
// the suggestion when the id is still the one that was asked about. Accept sets Accepted
// for that same id. A newer suggestion that arrives while the dialog is open is left as it
// is: not cleared, not accepted.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using TimeWarp.Architecture.Services;

partial class ProfileState
{
  public static class ConfirmProfileSuggestionActionSet
  {
    [CommandPaletteOptOut("Asks before a prefill replaces unsaved profile edits. The profile page runs it.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, IConfirmationService confirmation) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ArgumentNullException.ThrowIfNull(action);
        string? id = ProfileState.ProfileSuggestionId;
        if (id is null || ProfileState.ProfileSuggestionJson is null)
        {
          return;
        }

        bool accepted = await confirmation.ConfirmAsync
        (
          "Replace unsaved edits?",
          "An agent drafted new profile values. Applying them replaces unsaved changes on this form.",
          "Apply draft",
          "Keep mine",
          cancellationToken
        );

        if (!string.Equals(ProfileState.ProfileSuggestionId, id, StringComparison.Ordinal))
        {
          return;
        }

        if (!accepted)
        {
          ProfileState.ProfileSuggestionJson = null;
          ProfileState.ProfileSuggestionId = null;
          ProfileState.ProfileSuggestionAccepted = false;
          return;
        }

        ProfileState.ProfileSuggestionAccepted = true;
      }
    }
  }
}
