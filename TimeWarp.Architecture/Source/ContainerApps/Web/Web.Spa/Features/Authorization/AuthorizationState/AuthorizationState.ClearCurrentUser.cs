namespace TimeWarp.Architecture.Features.Authorization;

partial class AuthorizationState
{
  public static class ClearCurrentUser
  {
    public sealed class Action : IBaseAction;

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        AuthorizationState.Initialize();
        return Task.CompletedTask;
      }
    }
  }
}
