#region Purpose
// ClaimPendingNicknameForPrompt: marks the pending "name your new passkey" prompt as owned by AddPasskeyPrompt's own form.
#endregion

#region Design
// Exactly one surface may show the nickname editor for a just-added passkey (248-001 review M1):
// AddPasskeyPrompt renders on every page, including Settings, whose CredentialList auto-opens an
// inline editor for PendingListRenameCredentialId. When the prompt's CTA ran the ceremony it
// dispatches this BEFORE FetchCredentials so the list sees null and stays closed; the flag resets
// on rename success, ClearPendingNickname, and SetPendingNickname. Pure state mutation — no HTTP.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  public static class ClaimPendingNicknameForPromptActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store)
      {
      }

      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        _ = action;
        _ = cancellationToken;
        if (CredentialsState.PendingNicknameCredentialId is not null)
        {
          CredentialsState.PendingNicknameOwnedByPrompt = true;
        }

        return ValueTask.CompletedTask;
      }
    }
  }
}
