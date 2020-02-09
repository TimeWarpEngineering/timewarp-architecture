namespace TimeWarp.Blazor.BaseFeature
{
  using static TimeWarp.Blazor.ApplicationFeature.ApplicationState;

  public partial class ResetButton
  {
    internal void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}
