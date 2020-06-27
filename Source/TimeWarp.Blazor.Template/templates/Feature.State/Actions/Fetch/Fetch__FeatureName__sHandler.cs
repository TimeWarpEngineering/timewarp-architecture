namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;
  using System.Linq;
  using System;
  using Newtonsoft.Json;
  using static __RootNamespace__.Features.__FeatureName__s.__FeatureName__State;

  internal partial class __FeatureName__sState
  {
    public class Fetch__FeatureName__sHandler : BaseHandler<Fetch__FeatureName__sAction>
    {
      private readonly HttpClient HttpClient;

      public Fetch__FeatureName__sHandler(IStore aStore, HttpClient aHttpClient) : base(aStore)
      {
        HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        Fetch__FeatureName__sAction aFetch__FeatureName__sAction,
        CancellationToken aCancellationToken
      )
      {
        var get__FeatureName__sRequest = new Get__FeatureName__sRequest();
        HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(get__FeatureName__sRequest.RouteFactory);
        string json = await httpResponseMessage.Content.ReadAsStringAsync();
        Get__FeatureName__sResponse get__FeatureName__sResponse = JsonConvert.DeserializeObject<Get__FeatureName__sResponse>(json);

        __FeatureName__State.___FeatureName__Records = get__FeatureName__sResponse.__FeatureName__Records;

        return Unit.Value;
      }
    }
  }
}
