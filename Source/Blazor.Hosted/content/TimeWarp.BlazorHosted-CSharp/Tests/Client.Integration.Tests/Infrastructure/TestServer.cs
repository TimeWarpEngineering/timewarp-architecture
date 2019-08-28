namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using Microsoft.AspNetCore;
  using Microsoft.AspNetCore.Hosting;

  public class TestServer : Microsoft.AspNetCore.TestHost.TestServer
  {
    public TestServer() : base(WebHostBuilder()) { }

    private static IWebHostBuilder WebHostBuilder() =>
      WebHost.CreateDefaultBuilder()
      .UseStartup<Server.Startup>()
      .UseEnvironment("Local");
  }
}