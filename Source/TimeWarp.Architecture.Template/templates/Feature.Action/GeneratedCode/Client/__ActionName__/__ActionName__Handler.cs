namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;
  using System.Linq;
  using System;

  internal partial class __FeatureName__sState
  {
    public class __ActionName__Handler : BaseHandler<__ActionName__Action>
    {
      public __ActionName__Handler(IStore aStore) : base(aStore) { }

      public override async Task<Unit> Handle
      (
        __ActionName__Action a__ActionName__Action,
        CancellationToken aCancellationToken
      )
      {
        __FeatureName__State.PageIndex = a__ActionName__Action.IncreasePageIndex;
        return Unit.Value;
      }
    }
  }
}
