namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using FakeItEasy;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;
  using System;
  using TimeWarp.Blazor.Testing;
  using Microsoft.JSInterop;
  using TimeWarp.Blazor.Features.ClientLoaders;
  using MediatR;
  using System.Threading.Tasks;
  using System.Threading;
  using Microsoft.Extensions.Configuration;

  /// <summary>
  /// Creates the ServiceProvider for the Client and configures it on construction
  /// </summary>
  [NotTest]
  public class TestClientApplication
  {
    private readonly ISender ScopedSender;
    public IServiceProvider ServiceProvider { get; }

    public TestClientApplication(TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication)
    {
      var services = new ServiceCollection();
      // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
      services.AddSingleton(aTimeWarpBlazorServerApplication.HttpClient);

      ConfigureServices(services, aTimeWarpBlazorServerApplication.WebApplication.Configuration);
      ServiceProvider = services.BuildServiceProvider();
      ScopedSender = new ScopedSender(ServiceProvider);
    }

    private static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
    {
      Program.ConfigureServices(aServiceCollection, aConfiguration);

      // Theres is no JSRuntime in testing as we don't have an actual browser
      IJSRuntime fakeJsRuntime = A.Fake<IJSRuntime>();
      aServiceCollection.Replace(ServiceDescriptor.Scoped(_ => fakeJsRuntime));
      aServiceCollection.Replace(ServiceDescriptor.Scoped<IClientLoaderConfiguration, ClientLoaderTestConfiguration>());

      // Could replace ICurrentUserService here with a logged in one for tests that need to have logged in user.

      //ICurrentUserService fakeCurrentUserService = A.Fake<ICurrentUserService>();
      //A.CallTo(() => fakeCurrentUserService.IsAuthenticated).Returns(true);
      //A.CallTo(() => fakeCurrentUserService.Email).Returns(Constants.UserEmails.TrinsicUser);

      //aServiceCollection.Replace(ServiceDescriptor.Scoped(_ => fakeCurrentUserService));
    }

    public Task<TResponse> Send<TResponse>
    (
      IRequest<TResponse> aRequest,
      CancellationToken aCancellationToken = default
    ) => ScopedSender.Send(aRequest, aCancellationToken);

    public Task<object> Send(object aRequest, CancellationToken aCancellationToken = default) =>
      ScopedSender.Send(aRequest, aCancellationToken);

  }
}
