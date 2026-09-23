#region Purpose
// ClearPendingNickname: dismisses the "name your new passkey" prompt without renaming (skip / cancel).
#endregion

#region Design
// Pure state mutation — no HTTP. The credential keeps its provider label as the row title until the
// user renames it later from the list. Task 248-001.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  public static class ClearPendingNicknameActionSet
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
        CredentialsState.PendingNicknameCredentialId = null;
        CredentialsState.PendingNicknameDefault = null;
        CredentialsState.PendingNicknameOwnedByPrompt = false;
        return ValueTask.CompletedTask;
      }
    }
  }
}
