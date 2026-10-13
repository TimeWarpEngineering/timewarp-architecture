#region Purpose
// SessionChanged: forget pick mode, chips and sent tags when the signed-in principal changes.
#endregion

#region Design
// Dispatched by AuthenticationStateListener with AskReferenceState.PrincipalIdOf, next to the 345
// reference reset. A notification for the same principal keeps everything.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskTagState
{
  public static class SessionChangedActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string PrincipalId { get; }

      public Action(string principalId)
      {
        PrincipalId = principalId ?? throw new ArgumentNullException(nameof(principalId));
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (string.Equals(AskTagState.SessionPrincipalId, action.PrincipalId, StringComparison.Ordinal))
        {
          return ValueTask.CompletedTask;
        }

        AskTagState.SessionPrincipalId = action.PrincipalId;
        AskTagState.PickGeneration++;
        AskTagState.IsPicking = false;
        AskTagState.Attached = [];
        AskTagState.Sent = [];
        return ValueTask.CompletedTask;
      }
    }
  }
}
