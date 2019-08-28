namespace TimeWarp.Blazor.Client.Features.Counter
{
  using MediatR;
  using TimeWarp.Blazor.Api.Features.Base;

  public class IncrementCounterAction : BaseRequest, IRequest<CounterState>
  {
    public int Amount { get; set; }
  }
}