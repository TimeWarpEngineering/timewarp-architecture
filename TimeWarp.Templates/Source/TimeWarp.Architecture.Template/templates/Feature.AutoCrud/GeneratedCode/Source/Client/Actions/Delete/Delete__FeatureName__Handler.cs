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
    internal class Delete__FeatureName__Handler : BaseHandler<Delete__FeatureName__Action>
    {
      private readonly HttpClient HttpClient;
      private readonly JsonSerializerOptions JsonSerializerOptions;
      public Delete__FeatureName__Handler(
        IStore aStore,
        HttpClient aHttpClient,
        JsonSerializerOptions aJsonSerializerOptions) : base(aStore)
      {
        HttpClient = aHttpClient;
        JsonSerializerOptions = aJsonSerializerOptions;
      }
      public override async Task<Unit> Handle
      (
        Delete__FeatureName__Action aDelete__FeatureName__Action,
        CancellationToken aCancellationToken
      )
      {
        Console.WriteLine($"Delete Item Id : {aDelete__FeatureName__Action.Delete__FeatureName__Request.ItemId}");
        HttpResponseMessage httpResponseMessage =
          await HttpClient.PostAsJsonAsync<Delete__FeatureName__Request>
          (
            aDelete__FeatureName__Action.Delete__FeatureName__Request.GetRoute(),
            aDelete__FeatureName__Action.Delete__FeatureName__Request,
            aCancellationToken
          );
        httpResponseMessage.EnsureSuccessStatusCode();

        return await Unit.Task;
      }
    }

  }
}
