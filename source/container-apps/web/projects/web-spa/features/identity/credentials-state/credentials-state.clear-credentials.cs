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
  public static class ClearCredentialsActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        CredentialsState.Initialize();
        return default;
      }
    }
  }
}
