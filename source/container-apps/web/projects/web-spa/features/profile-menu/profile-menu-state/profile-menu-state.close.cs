#region Purpose
// CloseActionSet: dismisses the profile dropdown menu.
#endregion

#region Design
// Moves Open to Closing rather than straight to Closed so an animated dismissal can run
// before the menu is considered gone.
// The state guard makes close idempotent — safe to fire unconditionally from outside-click
// or loss-of-interest handlers regardless of the menu's phase.
#endregion

namespace TimeWarp.Architecture.Features.ProfileMenus;

partial class ProfileMenuState
{
  public static class CloseActionSet
  {
    internal class Action : IBaseAction;

    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) {}
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        if (ProfileMenuState.MenuState == MenuStates.Open)
        {
          ProfileMenuState.MenuState = MenuStates.Closing;
        }

        return Task.CompletedTask;
      }
    }
  }
}
