#region Purpose
// ClearCredentials: reset credential cache on sign-out (anonymous chrome).
#endregion

#region Design
// Mirrors ProfileState.ClearProfileData — Initialize is the single empty-shape path.
// Task 169.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  internal static class ClearCredentialsActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.Initialize();
        return Task.CompletedTask;
      }
    }
  }
}
