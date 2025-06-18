namespace TimeWarp.Architecture.Features.Profiles;

partial class ProfileState
{

  internal static class ClearProfileDataActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ProfileState.Initialize();
        return Task.CompletedTask;
      }
    }
  }
}
