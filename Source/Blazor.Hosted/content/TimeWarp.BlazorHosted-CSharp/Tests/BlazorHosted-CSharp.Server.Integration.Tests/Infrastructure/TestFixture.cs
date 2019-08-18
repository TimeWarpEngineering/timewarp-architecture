namespace BlazorHosted_CSharp.Server.Integration.Tests.Infrastructure
{
  using System;

  public class TestFixture
  {
    private readonly TestServer BlazorStateTestServer;

    /// <summary>
    /// This is the ServiceProvider that will be used by the Server
    /// </summary>
    public IServiceProvider ServiceProvider => BlazorStateTestServer.Services;

    public TestFixture(TestServer aBlazorStateTestServer)
    {
      BlazorStateTestServer = aBlazorStateTestServer;
    }
  }
}