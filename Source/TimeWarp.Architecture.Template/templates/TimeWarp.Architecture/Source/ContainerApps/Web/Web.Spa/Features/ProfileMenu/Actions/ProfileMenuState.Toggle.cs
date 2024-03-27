namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState
{
  public static class Toggle
  {
    internal class Action : BaseAction { }

    [UsedImplicitly]
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
