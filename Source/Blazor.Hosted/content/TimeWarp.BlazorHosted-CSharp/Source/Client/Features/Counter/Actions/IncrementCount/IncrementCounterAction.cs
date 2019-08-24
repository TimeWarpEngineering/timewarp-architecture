namespace BlazorHosted_CSharp.Client.Features.Counter
{
  using MediatR;
  using BlazorHosted_CSharp.Api.Features.Base;

  public class IncrementCounterAction : BaseRequest, IRequest<CounterState>
  {
    public int Amount { get; set; }
  }
}