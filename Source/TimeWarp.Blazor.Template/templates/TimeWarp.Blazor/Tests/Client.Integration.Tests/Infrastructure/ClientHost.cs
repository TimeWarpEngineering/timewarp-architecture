namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using FakeItEasy;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;
  using System;
  using TimeWarp.Blazor.Testing;
  using Microsoft.JSInterop;

  [NotTest]
  public class ClientHost
  {
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    public ClientHost(TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication)
    {
      var services = new ServiceCollection();
      // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
      services.AddSingleton(aTimeWarpBlazorServerApplication.HttpClient);
      ConfigureServices(services);

      ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection aServiceCollection)
    {
      Program.ConfigureServices(aServiceCollection);

      // Theres is no JSRuntime in testing as we don't have an actual browser
      IJSRuntime fakeJsRuntime = A.Fake<IJSRuntime>(); 
      aServiceCollection.Replace(ServiceDescriptor.Scoped(_ => fakeJsRuntime));

      // Could replace ICurrentUserService here with a logged in one for tests that need to have logged in user.

      //ICurrentUserService fakeCurrentUserService = A.Fake<ICurrentUserService>();
      //A.CallTo(() => fakeCurrentUserService.IsAuthenticated).Returns(true);
      //A.CallTo(() => fakeCurrentUserService.Email).Returns(Constants.UserEmails.TrinsicUser);

      //aServiceCollection.Replace(ServiceDescriptor.Scoped(_ => fakeCurrentUserService));
    }
  }
}
