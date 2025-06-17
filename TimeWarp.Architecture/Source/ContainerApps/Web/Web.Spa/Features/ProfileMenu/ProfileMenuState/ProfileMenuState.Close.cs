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
