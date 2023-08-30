namespace TimeWarp.Architecture.Features.ProfileMenus;

internal partial class ProfileMenuState
{
  public static class ToggleOpen
  {
    internal record Action : BaseAction { }
    internal class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store) { }

      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        ProfileMenuState.IsOpen = !ProfileMenuState.IsOpen;
        return Task.CompletedTask;
      }
    }
  }
}
