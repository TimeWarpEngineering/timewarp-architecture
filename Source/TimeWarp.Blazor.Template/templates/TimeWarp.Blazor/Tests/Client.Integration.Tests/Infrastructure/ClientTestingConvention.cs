namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using BlazorState;
  using Fixie;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.DependencyInjection;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Net.Http;
  using System.Reflection;
  using System.Text.Json;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.ClientLoaders;
  using TimeWarp.Blazor.Testing;


  //[NotTest]
  //public class TestingConvention : Discovery, Execution, IDisposable
  //{
  //  private readonly IServiceScopeFactory ServiceScopeFactory;
  //  private HttpClient ServerHttpClient;
  //  private WebApplicationFactory<Server.Startup> ServerWebApplicationFactory;

  //  public TestingConvention()
  //  {
  //    var testServices = new ServiceCollection();
  //    ConfigureTestServices(testServices);
  //    ServiceProvider serviceProvider = testServices.BuildServiceProvider();
  //    ServiceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();

  //    Classes.Where(aType => aType.IsPublic && !aType.Has<NotTest>());
  //    Methods.Where(aMethodInfo => aMethodInfo.Name != nameof(Setup) && !aMethodInfo.IsSpecialName);
  //  }

  //  public void Execute(TestClass aTestClass)
  //  {
  //    aTestClass.RunCases
  //    (
  //      aCase =>
  //      {
  //        using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
  //        object instance = serviceScope.ServiceProvider.GetService(aTestClass.Type);
  //        Setup(instance);
  //        aCase.Execute(instance);
  //        instance.Dispose();
  //      }
  //     );
  //  }

  //  private static void Setup(object aInstance)
  //  {
  //    MethodInfo methodInfo = aInstance.GetType().GetMethod(nameof(Setup));
  //    methodInfo?.Execute(aInstance);
  //  }

  //  private void ConfigureTestServices(ServiceCollection aServiceCollection)
  //  {
  //    ServerWebApplicationFactory = new WebApplicationFactory<Server.Startup>();
  //    ServerHttpClient = ServerWebApplicationFactory.CreateClient();

  //    ConfigureWebAssemblyHost(aServiceCollection);

  //    aServiceCollection.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });


  //    aServiceCollection.Scan
  //    (
  //      aTypeSourceSelector => aTypeSourceSelector
  //        .FromAssemblyOf<TestingConvention>()
  //        .AddClasses(action: (aClasses) => aClasses.Where(aType => aType.IsPublic && !aType.Has<NotTest>()))
  //        .AsSelf()
  //        .WithScopedLifetime()
  //    );
  //  }

  //  private void ConfigureWebAssemblyHost(ServiceCollection aServiceCollection)
  //  {
  //    //var webAssemblyHostBuilder = WebAssemblyHostBuilder.CreateDefault();
  //    //ConfigureServices(webAssemblyHostBuilder.Services);

  //    //WebAssemblyHost webAssemblyHost = webAssemblyHostBuilder.Build();
  //    //aServiceCollection.AddSingleton(webAssemblyHost);

  //    var clientHostBuilder = ClientHostBuilder.CreateDefault();
  //    ConfigureServices(clientHostBuilder.Services);

  //    ClientHost clientHost = clientHostBuilder.Build();
  //    aServiceCollection.AddSingleton(clientHost);

  //  }

  //  private void ConfigureServices(IServiceCollection aServiceCollection)
  //  {
  //    // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
  //    aServiceCollection.AddSingleton(ServerHttpClient);
  //    aServiceCollection.AddBlazorState
  //    (
  //      aOptions => aOptions.Assemblies =
  //      new Assembly[] { typeof(TimeWarp.Blazor.Client.Program).GetTypeInfo().Assembly }
  //    );

  //    aServiceCollection.AddSingleton
  //    (
  //      new JsonSerializerOptions
  //      {
  //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  //      }
  //    );

  //    aServiceCollection.AddSingleton<IClientLoaderConfiguration, ClientLoaderTestConfiguration>();
  //  }

  //  private bool DisposedValue;

  //  protected virtual void Dispose(bool aIsDisposing)
  //  {
  //    if (!DisposedValue)
  //    {
  //      if (aIsDisposing)
  //      {
  //        ServerWebApplicationFactory.Dispose();
  //      }

  //      DisposedValue = true;
  //    }
  //  }

  //  public void Dispose() => Dispose(true);
  //}

  [NotTest]
  public class ClientTestConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment) =>
      aTestConfiguration.Conventions.Add<TestDiscovery, TestExecution>();
  }

  //[NotTest]
  //public class TimeWarpDiscovery : IDiscovery
  //{
  //  public IEnumerable<Type> TestClasses(IEnumerable<Type> aConcreteClasses) =>
  //    aConcreteClasses.Where(aType => aType.IsPublic && !aType.Has<NotTest>());

  //  public IEnumerable<MethodInfo> TestMethods(IEnumerable<MethodInfo> aPublicMethods) =>
  //    aPublicMethods.Where(aMethodInfo => aMethodInfo.Name != "Setup" && !aMethodInfo.IsSpecialName);
  //}

  [NotTest]
  public class TimeWarpExecution : IExecution
  {
    private readonly IServiceScopeFactory ServiceScopeFactory;
    private HttpClient ServerHttpClient;
    private WebApplicationFactory<Server.Startup> ServerWebApplicationFactory;

    public TimeWarpExecution()
    {
      var testServices = new ServiceCollection();
      ConfigureTestServices(testServices);
      ServiceProvider serviceProvider = testServices.BuildServiceProvider();
      ServiceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
    }

    public async Task Run(TestSuite aTestSuite)
    {
      foreach (TestClass testClass in aTestSuite.TestClasses)
      {
        foreach (Test test in testClass.Tests)
        {
          using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
          object instance = serviceScope.ServiceProvider.GetService(testClass.Type);
          //object instance = testClass.Construct();

          MethodInfo method = testClass.Type.GetMethod("SetUp");
          if (method != null)
            await method.Call(instance);

          await test.Run(instance);
        }
      }
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
          .FromAssemblyOf<TimeWarpExecution>()
          .AddClasses(action: (aClasses) => aClasses.Where(aType => aType.IsPublic && !aType.Has<NotTest>()))
          .AsSelf()
          .WithScopedLifetime()
      );
    }

    private void ConfigureWebAssemblyHost(ServiceCollection aServiceCollection)
    {
      //var webAssemblyHostBuilder = WebAssemblyHostBuilder.CreateDefault();
      //ConfigureServices(webAssemblyHostBuilder.Services);

      //WebAssemblyHost webAssemblyHost = webAssemblyHostBuilder.Build();
      //aServiceCollection.AddSingleton(webAssemblyHost);

      var clientHostBuilder = ClientHostBuilder.CreateDefault();
      ConfigureServices(clientHostBuilder.Services);

      ClientHost clientHost = clientHostBuilder.Build();
      aServiceCollection.AddSingleton(clientHost);

    }

    private void ConfigureServices(IServiceCollection aServiceCollection)
    {
      // Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
      aServiceCollection.AddSingleton(ServerHttpClient);
      aServiceCollection.AddBlazorState
      (
        aOptions => aOptions.Assemblies =
        new Assembly[] { typeof(TimeWarp.Blazor.Client.Program).GetTypeInfo().Assembly }
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
  }
}
