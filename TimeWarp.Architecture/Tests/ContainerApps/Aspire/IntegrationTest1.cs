namespace Aspire.Tests;

public class IntegrationTest1
{
  // Instructions:
  // 1. Add a project reference to the target AppHost project, e.g.:
  //
  //    <ItemGroup>
  //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
  //    </ItemGroup>
  //
  // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
  // 
  [Fact]
  public async Task GetWebResourceRootReturnsOkStatusCode()
  {
    // Arrange
    IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Aspire_AppHost>();
    await using DistributedApplication app = await appHost.BuildAsync();
    await app.StartAsync();

    // Act
    HttpClient httpClient = app.CreateHttpClient("api-server");
    HttpResponseMessage response = await httpClient.GetAsync("api/weatherForecasts?Days=10");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }
}
