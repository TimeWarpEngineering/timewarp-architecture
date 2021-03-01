namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;

  internal partial class __FeatureName__State
  {
    public class __FeatureName__FetchHandler : BaseHandler<__FeatureName__FetchAction>
    {
      private readonly HttpClient HttpClient;

      public __FeatureName__FetchHandler(IStore aStore, HttpClient aHttpClient) : base(aStore)
      {
        HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        __FeatureName__FetchAction a__FeatureName__FetchAction,
        CancellationToken aCancellationToken
      )
      {
        //var a__FeatureName__ReadRequest = new __FeatureName__ReadRequest { ItemNumber = 10 };

        __FeatureName__GetResponse get__FeatureName__sResponse =
          await HttpClient.GetFromJsonAsync<__FeatureName__GetResponse>
          (
            __FeatureName__GetRequest.GetRoute(), aCancellationToken
          )
          .ConfigureAwait(false);
        List<__FeatureName__Dto> __FeatureName__s = get__FeatureName__sResponse.__FeatureName__s;
        __FeatureName__State.___FeatureName__s.Clear();
        __FeatureName__State.___FeatureName__s = __FeatureName__s.ToDictionary(
              a__FeatureName__ => a__FeatureName__.Id,
              __FeatureName__s
        );
        return Unit.Value;
      }
    }
  }
}
