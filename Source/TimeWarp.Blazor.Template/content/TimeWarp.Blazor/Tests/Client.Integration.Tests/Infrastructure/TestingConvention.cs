namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using BlazorState;
  using Fixie;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.DependencyInjection;
  using System;
  using System.Net.Http;
  using System.Reflection;
  using System.Text.Json;
  using TimeWarp.Blazor.Client.ClientLoaderFeature;

  public class TestingConvention : Discovery, Execution, IDisposable
  {
    const string TestPostfix = "Tests";
    private readonly IServiceScopeFactory ServiceScopeFactory;
    private HttpClient ServerHttpClient;
    private WebApplicationFactory<Server.Startup> ServerWebApplicationFactory;

    public TestingConvention()
    {
      var testServices = new ServiceCollection();
      ConfigureTestServices(testServices);
      ServiceProvider serviceProvider = testServices.BuildServiceProvider();
      ServiceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();

      Classes.Where(aType => aType.Name.EndsWith(TestPostfix));
      Methods.Where(aMethodInfo => aMethodInfo.Name != nameof(Setup));
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
          instance.Dispose();
        }
       );
    }

    private static void Setup(object aInstance)
    {
      MethodInfo methodInfo = aInstance.GetType().GetMethod(nameof(Setup));
      methodInfo?.Execute(aInstance);
    }

    private void ConfigureTestServices(ServiceCollection aServiceCollection)
    {
      ServerWebApplicationFactory = new WebApplicationFactory<Server.Startup>();
      ServerHttpClient = ServerWebApplicationFactory.CreateClient();

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
    }

    private bool DisposedValue;

    protected virtual void Dispose(bool aIsDisposing)
    {
      if (!DisposedValue)
      {
        if (aIsDisposing)
        {
          ServerWebApplicationFactory.Dispose();
        }

        DisposedValue = true;
      }
    }

    public void Dispose() => Dispose(true);
  }
}
