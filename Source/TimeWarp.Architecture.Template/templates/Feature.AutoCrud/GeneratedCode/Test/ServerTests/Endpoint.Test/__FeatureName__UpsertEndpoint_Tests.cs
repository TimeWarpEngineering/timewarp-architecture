namespace __RequestName__Endpoint
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
    private readonly __RequestName__Request __RequestName__Request;

    public Returns
    (
      WebApplicationFactory<Startup> aWebApplicationFactory,
      JsonSerializerOptions aJsonSerializerOptions
    ) : base(aWebApplicationFactory, aJsonSerializerOptions)
    {
      __RequestName__Request = new __RequestName__Request { SampleProperty = "sample" };
    }

    public async Task __RequestName__Response()
    {
      __RequestName__Response __RequestName__Response =
        await GetJsonAsync<__RequestName__Response>(__RequestName__Request.GetRoute());

      Validate__RequestName__Response(__RequestName__Response);
    }

    public async Task ValidationError()
    {
      // Set invalid value
      __RequestName__Request.SampleProperty = string.Empty;

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