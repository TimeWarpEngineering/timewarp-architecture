namespace __RootspaceName__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using eShopOnBlazorWasm.Features.Bases;
  using System;
  using System.Text.Json;
  internal partial class __FeatureName__State
  {
    public class __FeatureName__CreateHandler : BaseHandler<__FeatureName__CreateAction> {

      private readonly HttpClient HttpClient;
      private readonly JsonSerializerOptions JsonSerializerOptions;

      public __FeatureName__CreateHandler
      (
        IStore aStore,
        HttpClient aHttpClient,
        JsonSerializerOptions aJsonSerializerOptions
      ) : base(aStore)
      {
        HttpClient = aHttpClient;
        JsonSerializerOptions = aJsonSerializerOptions;
      }

      public override async Task<Unit> Handle
      (
        __FeatureName__CreateAction a__FeatureName__CreateAction,
        CancellationToken aCancellationToken
      )
      {
        Console.WriteLine($"Name:{a__FeatureName__CreateAction.__FeatureName__CreateRequest.Name}");
        HttpResponseMessage httpResponseMessage =
          await HttpClient.PostAsJsonAsync<__FeatureName__CreateRequest>
          (
            a__FeatureName__CreateAction.__FeatureName__CreateRequest.GetRoute(),
            a__FeatureName__CreateAction.__FeatureName__CreateRequest,
            aCancellationToken
          );

        httpResponseMessage.EnsureSuccessStatusCode();

        string json = await httpResponseMessage.Content.ReadAsStringAsync();

        Console.WriteLine("==============");
        Console.WriteLine(json);

        return Unit.Value;
      }
    }
  }
}