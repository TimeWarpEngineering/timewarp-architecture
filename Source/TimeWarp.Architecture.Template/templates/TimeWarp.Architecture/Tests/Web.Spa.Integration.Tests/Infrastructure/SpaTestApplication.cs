namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.ClientLoaders;
using TimeWarp.Architecture.Testing;

/// <summary>
/// Inject this when the SUT is the Web.Spa
/// Creates the ServiceProvider for the Spa and configures it on construction
/// </summary>
[NotTest]
public class SpaTestApplication // Maybe we make this generic passing in the WebApplication we want to use for the URL??
{
  private readonly ISender ScopedSender;
  public IServiceProvider ServiceProvider { get; }

  public SpaTestApplication(WebServerApplication aWebServerApplication)
  {
    var services = new ServiceCollection();
    // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
    // If using Yarp we want its HttpClient 
    services.AddSingleton(aWebServerApplication.HttpClient);

    ConfigureServices(services, aWebServerApplication.WebApplicationHost.Configuration);
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
