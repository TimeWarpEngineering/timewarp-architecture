namespace __RequestName__Endpoint
{
  using FluentAssertions;
  using Microsoft.AspNetCore.Mvc.Testing;
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
      __RequestName__Request = new __RequestName__Request { };
    }

    public async Task __RequestName__Response()
    {
      __RequestName__Response __RequestName__Response =
        await GetJsonAsync<__RequestName__Response>(__RequestName__Request.RouteFactory);

      Validate__RequestName__Response(__RequestName__Response);
    }

    public async Task ValidationError()
    {
      // Set invalid value
      // __RequestName__Request.Days = -1;

      HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(__RequestName__Request.RouteFactory);

      string json = await httpResponseMessage.Content.ReadAsStringAsync();

      httpResponseMessage.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
      json.Should().Contain("errors");
      //json.Should().Contain(nameof(__RequestName__Request.??SomeParam??));
    }

    private void Validate__RequestName__Response(__RequestName__Response a__RequestName__Response)
    {
      a__RequestName__Response.RequestId.Should().Be(__RequestName__Request.Id);
      // check Other properties here
    }
  }
}