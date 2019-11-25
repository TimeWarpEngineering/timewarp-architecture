namespace TimeWarp.Blazor.Client.BaseFeature
{
  using static TimeWarp.Blazor.Client.ApplicationFeature.ApplicationState;

  public partial class ResetButton
  {
    internal void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}
