namespace TimeWarp.Blazor.Client.Features.Base.Components
{
  using TimeWarp.Blazor.Client.Features.Application;

  public class ResetButtonBase : BaseComponent
  {
    protected void ButtonClick() => Mediator.Send(new ResetStoreAction());
  }
}