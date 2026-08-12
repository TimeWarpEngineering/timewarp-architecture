#region Purpose
// ToggleActionSet: flips the profile menu between open and closed.
#endregion

#region Design
// Toggling during Opening/Closing is deliberately a no-op so rapid clicks cannot corrupt
// an in-flight transition; the throwing default surfaces any enum value added without a
// matching transition. Routing toggles through the transitional states is blocked on the
// inline TODO (transitions and NotifyLossOfInterest).
#endregion

namespace TimeWarp.Architecture.Features.ProfileMenus;

partial class ProfileMenuState
{
  public static class ToggleActionSet
  {
    internal class Action : IBaseAction;

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ProfileMenuState.MenuState = ProfileMenuState.MenuState switch
        {
          // TODO: Transitions and NotifyLossOfInterest not working
          // MenuStates.Closed => MenuStates.Opening,
          // MenuStates.Open => MenuStates.Closing,
          MenuStates.Closed => MenuStates.Open,
          MenuStates.Open => MenuStates.Closed,
          MenuStates.Closing => MenuStates.Closing, // Do nothing
          MenuStates.Opening => MenuStates.Opening, // Do nothing
          _ => throw new NotImplementedException()
        };

        return Task.CompletedTask;
      }
    }
  }
}
