#region Purpose
// ClearProfileDataActionSet: resets profile data to anonymous defaults (sign-out path).
#endregion

#region Design
// Delegates to ProfileState.Initialize() so the signed-out state and the app-startup
// state share one code path and cannot drift apart.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

partial class ProfileState
{

  public static class ClearProfileDataActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ProfileState.Initialize();
        return default;
      }
    }
  }
}
