namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState
{
  public static class Toggle
  {
    internal record Action : BaseAction { }
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ProfileMenuState.MenuState = ProfileMenuState.MenuState switch
        {
          MenuStates.Closed => MenuStates.Opening,
          MenuStates.Open => MenuStates.Closing,
          MenuStates.Closing => MenuStates.Closing, // Do nothing
          MenuStates.Opening => MenuStates.Opening, // Do nothing
          _ => throw new NotImplementedException()
        };

        return Task.CompletedTask;
      }
    }
  }
}
