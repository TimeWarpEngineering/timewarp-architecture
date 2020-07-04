namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;
  using System.Linq;
  using System;
    using static __RootNamespace__.Features.__FeatureName__s.__FeatureName__State;

  internal partial class __FeatureName__sState
  {
    public class __ActionName__sHandler : BaseHandler<__ActionName__sAction>
    {

      public __ActionName__sHandler(IStore aStore, HttpClient aHttpClient) : base(aStore)
      {

      }

      public override async Task<Unit> Handle
      (
        __ActionName__sAction a__ActionName__sAction,
        CancellationToken aCancellationToken
      )
      {

        return Unit.Value;
      }
    }
  }
}
