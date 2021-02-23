namespace __RootspaceName__.Features.__FeatureName__s
{
  using BlazorState;
  using MediatR;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using __RootspaceName__.Features.Bases;
  using System;
  using System.Text.Json;
  internal partial class __FeatureName__State
  {
    public class Delete__FeatureName__Handler : BaseHandler<Delete__FeatureName__Action>
    {
      private readonly HttpClient HttpClient;
      public Delete__FeatureName__Handler
      (
        IStore aStore,
        HttpClient aHttpClient
      ) : base(aStore)
      {
        HttpClient = aHttpClient;
      }

      public override async Task<Unit> Handle
      (
        Delete__FeatureName__Action aDelete__FeatureName__Action,
        CancellationToken aCancellationToken
      )
      {
        var delete__FeatureName__Request = new __FeatureName__DeleteRequest
        {
          ItemId = aDelete__FeatureName__Action.ItemId;
        };
        HttpResponseMessage httpResponseMessage =
        await HttpClient.DeleteAsync(delete__FeatureName__Request.RouteFactory);
        httpResponseMessage.EnsureSuccessStatusCode();
        __FeatureName__State._CatalogItems.Remove(aDelete__FeatureName__Action.ItemId);

        return Unit.Value;
      }
    }
  }
}