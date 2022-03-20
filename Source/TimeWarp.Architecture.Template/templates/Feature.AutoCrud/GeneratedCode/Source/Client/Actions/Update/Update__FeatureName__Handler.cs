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
    internal class Update__FeatureName__Handler : BaseHandler<Update__FeatureName__Action>
    {
      private readonly HttpClient HttpClient;
      private readonly JsonSerializerOptions JsonSerializerOptions;
      public Update__FeatureName__Handler(
        IStore aStore,
        HttpClient aHttpClient,
        JsonSerializerOptions aJsonSerializerOptions) : base(aStore)
      {
        HttpClient = aHttpClient;
        JsonSerializerOptions = aJsonSerializerOptions;
      }
      public override async Task<Unit> Handle
      (
        Update__FeatureName__Action aUpdate__FeatureName__Action,
        CancellationToken aCancellationToken
      )
      {
        Console.WriteLine($"Update Name : {aUpdate__FeatureName__Action.Upsert__FeatureName__Request.Name}");
        HttpResponseMessage httpResponseMessage =
          await HttpClient.PostAsJsonAsync<Upsert__FeatureName__Request>
          (
            aUpdate__FeatureName__Action.Upsert__FeatureName__Request.GetRoute(),
            aUpdate__FeatureName__Action.Upsert__FeatureName__Request,
            aCancellationToken
          );
        httpResponseMessage.EnsureSuccessStatusCode();

        return await Unit.Task;
      }
    }

  }
}
