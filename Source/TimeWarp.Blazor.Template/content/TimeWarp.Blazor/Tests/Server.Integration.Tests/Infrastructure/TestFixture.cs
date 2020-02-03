namespace TimeWarp.Blazor.Server.Integration.Tests.Infrastructure
{
  using System;

  public class TestFixture
  {
    private readonly TestServer TestServer;

    /// <summary>
    /// This is the ServiceProvider that will be used by the Server
    /// </summary>
    public IServiceProvider ServiceProvider => TestServer.Services;

    public TestFixture(TestServer aTestServer)
    {
      TestServer = aTestServer;
    }
  }
}
