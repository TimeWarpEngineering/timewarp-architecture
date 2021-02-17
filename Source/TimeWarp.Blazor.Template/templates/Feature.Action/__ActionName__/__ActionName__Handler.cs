namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;
  using System.Linq;
  using System;
  using System.Net.Http;

  internal partial class __FeatureName__sState
  {
    public class __ActionName__Handler : BaseHandler<__ActionName__Action>
    {
      private readonly HttpClient HttpClient;
      public __ActionName__Handler(IStore aStore, HttpClient aHttpClient) : base(aStore)
      {
          HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        __ActionName__Action a__ActionName__Action,
        CancellationToken aCancellationToken
      )
      {
        return Unit.Value;
      }
    }
  }
}
