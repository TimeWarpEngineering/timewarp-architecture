namespace __RootNamespace__.Features.__FeatureName__s
{
  using __RootNamespace__.Features.Bases;
  using BlazorState;
  using MediatR;
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;

  internal partial class __FeatureName__State
  {
    public class Save__FeatureName__Handler : BaseHandler<Save__FeatureName__Action>
    {
      private readonly HttpClient HttpClient;

      public Save__FeatureName__Handler
      (
        IStore aStore,
        HttpClient aHttpClient
      ) : base(aStore)
      {
        HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        Save__FeatureName__Action aSave__FeatureName__Action,
        CancellationToken aCancellationToken
      )
      {
        Save__FeatureName__Request save__FeatureName__Request = 
          aSave__FeatureName__Action.Save__FeatureName__Request;

        HttpResponseMessage httpResponseMessage =
          await HttpClient.PostAsJsonAsync<Save__FeatureName__Request>
          (
            save__FeatureName__Request.GetRoute(),
            save__FeatureName__Request
          );

        httpResponseMessage.EnsureSuccessStatusCode();

        return Unit.Value;
      }
    }
  }
}
