#region Purpose
// DismissPasskeySoftPrompt: hide the Entra-session add-passkey banner for this SPA session.
#endregion

#region Design
// In-memory flag only. "Later" persistence (sessionStorage) lives on AddPasskeyPrompt so this
// ActionSet stays host-free. Logout Initialize() resets the flag; the prompt component also
// removes the later key so a following principal on the same tab is not suppressed.
// RFC 219 D8: dismiss is UX, never a route or session gate.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  internal static class DismissPasskeySoftPromptActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.PasskeySoftPromptDismissed = true;
        return Task.CompletedTask;
      }
    }
  }
}
