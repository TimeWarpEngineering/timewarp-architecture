namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;
  using System.Collections.Generic;
  using System.Linq;

  internal partial class __FeatureName__State
  {
    public class Fetch__FeatureName__Handler : BaseHandler<Fetch__FeatureName__Action>
    {
      private readonly HttpClient HttpClient;

      public Fetch__FeatureName__Handler(IStore aStore, HttpClient aHttpClient) : base(aStore)
      {
        HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        Fetch__FeatureName__Action aFetch__FeatureName__Action,
        CancellationToken aCancellationToken
      )
      {
        //var a__FeatureName__ReadRequest = new __FeatureName__ReadRequest { ItemNumber = 10 };
        var Get__FeatureName__sRequest = new GetAll__FeatureName__Request();
        GetAll__FeatureName__Response get__FeatureName__sResponse = await HttpClient.GetFromJsonAsync<GetAll__FeatureName__Response>(Get__FeatureName__sRequest.GetRoute(), aCancellationToken).ConfigureAwait(false);
        List<__FeatureName__Dto> __FeatureName__s = get__FeatureName__sResponse.Cars;
        __FeatureName__State.___FeatureName__s.Clear();
        __FeatureName__State.___FeatureName__s = __FeatureName__s.ToDictionary(
              a__FeatureName__ => a__FeatureName__.Id,
              a__FeatureName__ => a__FeatureName__
        );
        return Unit.Value;
      }
    }
  }
}
