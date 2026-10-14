#region Purpose
// SessionChanged: forget pick mode, chips and sent tags when the signed-in principal changes.
#endregion

#region Design
// Dispatched by AuthenticationStateListener with AskReferenceState.PrincipalIdOf, next to the 345
// reference reset. A notification for the same principal keeps everything. A change with a pick in
// flight (armed or arming) also tells the browser to leave pick mode (AskTagJsModule.CancelPickAsync),
// so the overlay and its capture listeners do not outlive the principal.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Services;

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
      IStore store,
      IJSRuntime jsRuntime
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (string.Equals(AskTagState.SessionPrincipalId, action.PrincipalId, StringComparison.Ordinal))
        {
          return;
        }

        string? pickToken = AskTagState.PickToken;
        AskTagState.SessionPrincipalId = action.PrincipalId;
        AskTagState.PickToken = null;
        AskTagState.Attached = [];
        AskTagState.Sent = [];
        if (pickToken is not null)
        {
          await AskTagJsModule.CancelPickAsync(jsRuntime, pickToken);
        }

        AskTagState.IsPicking = false;
      }
    }
  }
}
