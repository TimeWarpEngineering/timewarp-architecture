namespace __RootNamespace__.Features.__FeatureName__s
{
  using __RootNamespace__.Features.Bases;
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using System;
  using System.Text.Json;

  internal partial class __FeatureName__State
  {
    internal class __FeatureName__DeleteHandler : BaseHandler<__FeatureName__DeleteAction>
    {
      private readonly HttpClient HttpClient;
      private readonly JsonSerializerOptions JsonSerializerOptions;
      public __FeatureName__DeleteHandler(
        IStore aStore,
        HttpClient aHttpClient,
        JsonSerializerOptions aJsonSerializerOptions) : base(aStore)
      {
        HttpClient = aHttpClient;
        JsonSerializerOptions = aJsonSerializerOptions;
      }
      public override async Task<Unit> Handle
      (
        __FeatureName__DeleteAction a__FeatureName__DeleteAction,
        CancellationToken aCancellationToken
      )
      {
        Console.WriteLine($"Delete Item Id : {a__FeatureName__DeleteAction.__FeatureName__DeleteRequest.ItemId}");
        HttpResponseMessage httpResponseMessage =
          await HttpClient.PostAsJsonAsync<__FeatureName__DeleteRequest>
          (
            a__FeatureName__DeleteAction.__FeatureName__DeleteRequest.GetRoute(),
            a__FeatureName__DeleteAction.__FeatureName__DeleteRequest,
            aCancellationToken
          );
        httpResponseMessage.EnsureSuccessStatusCode();

        return await Unit.Task;
      }
    }

  }
}
