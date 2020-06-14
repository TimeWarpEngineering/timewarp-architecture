namespace __RequestName__Handler
{
  using System.Threading.Tasks;
  using System.Text.Json;
  using Microsoft.AspNetCore.Mvc.Testing;
  using __RootNamespace__.Server.Integration.Tests.Infrastructure;
  using __RootNamespace__.Features.__FeatureName__s;
  using __RootNamespace__.Server;
  using FluentAssertions;

  public class Handle_Returns : BaseTest
  {
    private readonly __RequestName__Request __RequestName__Request;

    public Handle_Returns
    (
      WebApplicationFactory<Startup> aWebApplicationFactory,
      JsonSerializerOptions aJsonSerializerOptions
    ) : base(aWebApplicationFactory, aJsonSerializerOptions)
    {
      __RequestName__Request = new __RequestName__Request { Days = 10 };
    }

    public async Task __RequestName__Response()
    {
      __RequestName__Response __RequestName__Response = await Send(__RequestName__Request);

      Validate__RequestName__Response(__RequestName__Response);
    }

    private void Validate__RequestName__Response(__RequestName__Response a__RequestName__Response)
    {
      a__RequestName__Response.CorrelationId.Should().Be(__RequestName__Request.CorrelationId);
      // check Other properties here
    }

  }
}