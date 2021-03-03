namespace __RootNamespace__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.Bases;
  using System;
  using System.Text.Json;
  internal partial class __FeatureName__State
  {
    public class Create__FeatureName__Handler : BaseHandler<Create__FeatureName__Action> {

      private readonly HttpClient HttpClient;
      private readonly JsonSerializerOptions JsonSerializerOptions;

      public Create__FeatureName__Handler
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
        Create__FeatureName__Action aCreate__FeatureName__Action,
        CancellationToken aCancellationToken
      )
      {
        Console.WriteLine($"Name:{aCreate__FeatureName__Action.Upsert__FeatureName__Request.Name}");
        HttpResponseMessage httpResponseMessage =
          await HttpClient.PostAsJsonAsync<Upsert__FeatureName__Request>
          (
            aCreate__FeatureName__Action.Upsert__FeatureName__Request.GetRoute(),
            aCreate__FeatureName__Action.Upsert__FeatureName__Request,
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