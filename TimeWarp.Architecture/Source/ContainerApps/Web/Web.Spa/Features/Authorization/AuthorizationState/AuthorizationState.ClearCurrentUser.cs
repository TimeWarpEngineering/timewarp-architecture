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
