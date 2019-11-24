namespace TimeWarp.Blazor.Client.Features.Base.Components
{
  using static TimeWarp.Blazor.Client.ApplicationFeature.ApplicationState;

  public partial class ResetButton
  {
    internal void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}