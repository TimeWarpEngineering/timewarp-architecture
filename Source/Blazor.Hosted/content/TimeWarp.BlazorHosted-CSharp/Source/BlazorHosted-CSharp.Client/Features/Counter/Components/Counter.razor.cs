namespace BlazorHosted_CSharp.Client.Features.Counter.Components
{
  using BlazorHosted_CSharp.Client.Features.Base.Components;
  using BlazorHosted_CSharp.Client.Features.Counter;
  using System.Threading.Tasks;

  public class CounterModel : BaseComponent
  {
    internal async Task ButtonClick() =>
      _ = await Mediator.Send(new IncrementCounterAction { Amount = 5 });
  }
}