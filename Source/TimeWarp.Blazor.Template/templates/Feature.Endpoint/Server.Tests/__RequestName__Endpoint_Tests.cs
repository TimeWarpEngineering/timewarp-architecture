namespace __RequestName__Endpoint
{
  using FluentAssertions;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Shouldly;
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

    private void Validate__RequestName__Response(__RequestName__Response a__RequestName__Response)
    {
      Assert.Equal(a__RequestName__Response.RequestId,__RequestName__Request.Id);
      a__RequestName__Response.RequestId.ShouldBe(__RequestName__Request.Id);
      a__RequestName__Response.RequestId.Should().Be(__RequestName__Request.Id);
    }

  }
}