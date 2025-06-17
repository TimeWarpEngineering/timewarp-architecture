namespace __FeatureName__GetEndpoint
{
  using FluentAssertions;
  using Microsoft.AspNetCore.Mvc.Testing;
  using System.Net;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading.Tasks;
  using __RootNamespace__.Features.__FeatureName__s;
  using __RootNamespace__.Server.Integration.Tests.Infrastructure;
  using __RootNamespace__.Server;

  public class Returns : BaseTest
  {
    private readonly __FeatureName__GetRequest __FeatureName__GetRequest;

    public Returns
    (
      WebApplicationFactory<Startup> aWebApplicationFactory,
      JsonSerializerOptions aJsonSerializerOptions
    ) : base(aWebApplicationFactory, aJsonSerializerOptions)
    {
      __FeatureName__GetRequest = new __FeatureName__GetRequest { PageSize = 5, PageIndex = 1 };
    }

    public async Task __FeatureName__GetResponse()
    {
      __FeatureName__GetResponse __FeatureName__GetResponse =
        await GetJsonAsync<__FeatureName__GetResponse>(__FeatureName__GetRequest.GetRoute());

      Validate__FeatureName__GetResponse(__FeatureName__GetResponse);
    }

    public async Task ValidationError()
    {
      // Set invalid value
      __FeatureName__GetRequest.PageSize = string.Empty;

      HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(__RequestName__Request.GetRoute());

      string json = await httpResponseMessage.Content.ReadAsStringAsync();

      httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
      json.Should().Contain("errors");
      json.Should().Contain(nameof(__RequestName__Request.SampleProperty));
    }

    private void Validate__RequestName__Response(__RequestName__Response a__RequestName__Response)
    {
      // check Other properties here
    }
  }
}