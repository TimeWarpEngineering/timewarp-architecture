namespace TimeWarp.Blazor.Features.Applications
{
  using BlazorState;
  using MediatR;
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;

  internal partial class ApplicationState
  {
    internal class FiveSecondTaskHandler : BaseHandler<FiveSecondTaskAction>
    {
      public FiveSecondTaskHandler(IStore aStore) : base(aStore) { }

      public override async Task<Unit> Handle(FiveSecondTaskAction aFiveSecondTaskAction, CancellationToken aCancellationToken)
      {
        Console.WriteLine("Start");
        await Task.Delay(millisecondsDelay: 5000, cancellationToken: aCancellationToken);
        Console.WriteLine("Done");
        return Unit.Value;
      }
    }
  }
}
