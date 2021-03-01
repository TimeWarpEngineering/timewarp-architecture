namespace __RootNamespace__.Features.__FeatureName__s
{
  using PhayaoSocial.Features.Bases;
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
    internal class __FeatureName__UpdateHandler : BaseHandler<__FeatureName__UpdateAction>
    {
      private readonly HttpClient HttpClient;
      private readonly JsonSerializerOptions JsonSerializerOptions;
      public __FeatureName__UpdateHandler(
        IStore aStore,
        HttpClient aHttpClient,
        JsonSerializerOptions aJsonSerializerOptions) : base(aStore)
      {
        HttpClient = aHttpClient;
        JsonSerializerOptions = aJsonSerializerOptions;
      }
      public override async Task<Unit> Handle
      (
        __FeatureName__UpdateAction a__FeatureName__UpdateAction,
        CancellationToken aCancellationToken
      )
      {
        Console.WriteLine($"Name:{a__FeatureName__UpdateAction.__FeatureName__UpsertRequest.Name}");
        HttpResponseMessage httpResponseMessage =
          await HttpClient.PatchAsync<__FeatureName__UpsertRequest>
          (
            a__FeatureName__UpdateAction.__FeatureName__UpsertRequest.GetRoute(),
            a__FeatureName__UpdateAction.__FeatureName__UpsertRequest,
            aCancellationToken
          );
        httpResponseMessage.EnsureSuccessStatusCode();

        return await Unit.Task;
      }
    }

  }
}
