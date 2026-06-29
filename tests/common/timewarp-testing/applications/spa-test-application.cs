namespace TimeWarp.Architecture.Testing;

/// <summary>
/// Inject this when the SUT is the Web.Spa
/// Creates the ServiceProvider for the Spa and configures it on construction
/// </summary>
[NotTest]
public class SpaTestApplication<TViaTestServerApplication, TProgram> : ISpaTestApplication
  where TViaTestServerApplication : TestServerApplication<TProgram>
  where TProgram : IAspNetProgram
{
  private readonly ISender ScopedSender;
  public IServiceProvider ServiceProvider { get; }

  public SpaTestApplication(IServiceProvider testingServiceProvider)
  {
    var testServerApplication = (TViaTestServerApplication)testingServiceProvider.GetRequiredService(typeof(TViaTestServerApplication));
    var services = new ServiceCollection();

    // We need an HttpClient to talk to the Server side configured before calling AddTimeWarpState.
    // services.AddSingleton(testServerApplication.HttpClient);

    ConfigureServices(services, testServerApplication.WebApplicationHost.Configuration);
    string baseurl = testServerApplication.WebApplicationHost.Urls.First();
    services.AddHttpClient(TimeWarp.Foundation.Configuration.ServiceNames.ApiServiceName, c => c.BaseAddress = new Uri(baseurl));
    ServiceProvider = services.BuildServiceProvider();
    ScopedSender = new ScopedSender(ServiceProvider);
  }

  private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
  {
    Web.Spa.Program.ConfigureServices(serviceCollection, configuration);

    // Theres is no JSRuntime in testing as we don't have an actual browser
    IJSRuntime fakeJsRuntime = A.Fake<IJSRuntime>();
    serviceCollection.Replace(ServiceDescriptor.Scoped(_ => fakeJsRuntime));

    // Could replace ICurrentUserService here with a logged in one for tests that need to have logged in user.

    //ICurrentUserService fakeCurrentUserService = A.Fake<ICurrentUserService>();
    //A.CallTo(() => fakeCurrentUserService.IsAuthenticated).Returns(true);
    //A.CallTo(() => fakeCurrentUserService.Email).Returns(Constants.UserEmails.TrinsicUser);

    //serviceCollection.Replace(ServiceDescriptor.Scoped(_ => fakeCurrentUserService));
  }

  public Task<TResponse> Send<TResponse>
  (
    IRequest<TResponse> request,
    CancellationToken cancellationToken = default
  ) => ScopedSender.Send(request, cancellationToken);

  public Task<object> Send(object request, CancellationToken cancellationToken = default) =>
    ScopedSender.Send(request, cancellationToken);

}

public interface ISpaTestApplication
{
  IServiceProvider ServiceProvider { get; }
}
