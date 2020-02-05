namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using BlazorState;
  using Fixie;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.DependencyInjection;
  using System.Net.Http;
  using System.Reflection;
  using System.Text.Json;
  using TimeWarp.Blazor.Client.ApplicationFeature;
  using TimeWarp.Blazor.Client.ClientLoaderFeature;
  using TimeWarp.Blazor.Client.CounterFeature;
  using TimeWarp.Blazor.Client.EventStreamFeature;
  using TimeWarp.Blazor.Client.WeatherForecastFeature;

  public class TestingConvention : Discovery, Execution
  {
    const string TestPostfix = "Tests";
    private readonly IServiceScopeFactory ServiceScopeFactory;
    private HttpClient ServerHttpClient;

    public TestingConvention()
    {
      var testServices = new ServiceCollection();
      ConfigureTestServices(testServices);
      ServiceProvider serviceProvider = testServices.BuildServiceProvider();
      ServiceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();

      Methods.Where(aMethodExpression => aMethodExpression.Name != nameof(Setup));
    }

    public void Execute(TestClass aTestClass)
    {
      aTestClass.RunCases
      (
        aCase =>
        {
          using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
          object instance = serviceScope.ServiceProvider.GetService(aTestClass.Type);
          Setup(instance);
          aCase.Execute(instance);
        }
       );
    }

    private static void Setup(object aInstance)
    {
      System.Reflection.MethodInfo method = aInstance.GetType().GetMethod(nameof(Setup));
      method?.Execute(aInstance);
    }

    private void ConfigureTestServices(ServiceCollection aServiceCollection)
    {
      var serverWebApplicationFactory = new WebApplicationFactory<Server.Startup>();
      ServerHttpClient = serverWebApplicationFactory.CreateClient();

      ConfigureWebAssemblyHost(aServiceCollection);

      aServiceCollection.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });


      aServiceCollection.Scan
      (
        aTypeSourceSelector => aTypeSourceSelector
          // Start with all non abstract types in this assembly
          .FromAssemblyOf<TestingConvention>()
          // Add all the classes that end in Tests
          .AddClasses(action: (aClasses) => aClasses.TypeName().EndsWith(TestPostfix))
          .AsSelf()
          .WithScopedLifetime()
      );
    }

    private void ConfigureWebAssemblyHost(ServiceCollection aServiceCollection)
    {
      WebAssemblyHostBuilder WebAssemblyHostBuilder = WebAssemblyHostBuilder.CreateDefault();
      ConfigureServices(WebAssemblyHostBuilder.Services);


      WebAssemblyHost webAssemblyHost = WebAssemblyHostBuilder.Build();
      aServiceCollection.AddSingleton(webAssemblyHost);

    }

    private void ConfigureServices(IServiceCollection aServiceCollection)
    {
      // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
      aServiceCollection.AddSingleton(ServerHttpClient);
      aServiceCollection.AddBlazorState
      (
        aOptions => aOptions.Assemblies =
        new Assembly[] { typeof(Program).GetTypeInfo().Assembly }
      );

      aServiceCollection.AddSingleton
      (
        new JsonSerializerOptions
        {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }
      );

      aServiceCollection.AddSingleton<IClientLoaderConfiguration, ClientLoaderTestConfiguration>();
      aServiceCollection.AddTransient<ApplicationState>();
      aServiceCollection.AddTransient<CounterState>();
      aServiceCollection.AddTransient<EventStreamState>();
      aServiceCollection.AddTransient<WeatherForecastsState>();
    }
  }
}
