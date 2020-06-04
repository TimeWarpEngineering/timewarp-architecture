namespace TimeWarp.Blazor.Components
{
  using TimeWarp.Blazor.Features.Bases;
  using static TimeWarp.Blazor.Features.Applications.ApplicationState;

  public partial class ResetButton:BaseComponent
  {
    internal void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}
