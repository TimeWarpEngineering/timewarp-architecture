namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState
{
  public static class Close
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
        if (ProfileMenuState.MenuState == MenuStates.Open)
        {
          ProfileMenuState.MenuState = MenuStates.Closing;
        }
        return Task.CompletedTask;
      }
    }
  }
}
