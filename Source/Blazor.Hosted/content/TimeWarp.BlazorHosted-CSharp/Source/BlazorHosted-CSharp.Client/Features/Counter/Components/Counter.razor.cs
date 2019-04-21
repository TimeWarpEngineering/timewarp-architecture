namespace BlazorHosted_CSharp.Client.Features.Counter.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;
  using BlazorHosted_CSharp.Client.Features.Counter;

  public class CounterModel : BaseComponent
  {
    internal void ButtonClick() =>
      Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}