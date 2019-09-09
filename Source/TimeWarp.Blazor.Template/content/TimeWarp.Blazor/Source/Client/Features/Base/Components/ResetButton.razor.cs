namespace TimeWarp.Blazor.Client.Features.Base.Components
{
  using static TimeWarp.Blazor.Client.Features.Application.ApplicationState;

  public class ResetButtonBase : BaseComponent
  {
    internal void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}