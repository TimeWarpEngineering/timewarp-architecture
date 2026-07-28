#region Purpose
// AuthorizationState action that discards the user's module and role grants.
#endregion

#region Design
// Dispatched by AuthenticationStateListener when the user becomes unauthenticated, so a
// signed-out session (or the next identity) cannot act on grants fetched for the previous one.
// Delegates to Initialize so "cleared" is identical to "never fetched".
#endregion

namespace TimeWarp.Architecture.Features.Authorization;

partial class AuthorizationState
{
  internal static class ClearCurrentUserActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) {}
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        AuthorizationState.Initialize();
        return Task.CompletedTask;
      }
    }
  }
}
