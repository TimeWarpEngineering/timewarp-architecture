namespace TimeWarp.Blazor.Components
{
  using TimeWarp.Blazor.Features.Bases.Client;
  using static TimeWarp.Blazor.Features.Applications.Client.ApplicationState;

  public partial class ResetButton:BaseComponent
  {
    internal void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}
