namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState
{
  public static class Close
  {
    internal record Action : BaseAction { }
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

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
