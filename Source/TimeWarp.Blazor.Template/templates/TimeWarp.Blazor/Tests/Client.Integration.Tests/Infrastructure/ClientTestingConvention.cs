namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using Dawn;
  using Fixie;
  using Microsoft.Extensions.DependencyInjection;
  using System;
  using System.Reflection;
  using System.Text.Json;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Testing;

  [NotTest]
  public class ClientTestConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment)
    {
      var testDiscovery = new TestDiscovery(aTestEnvironment.CustomArguments);
      var testExecution = new TimeWarpExecution();

      aTestConfiguration.Conventions.Add(testDiscovery, testExecution);
    }
  }

  [NotTest]
  public class TimeWarpExecution : IExecution
  {
    private readonly ServiceProvider ServiceProvider;

    private readonly IServiceScopeFactory ServiceScopeFactory;
    //private HttpClient ServerHttpClient;
    //private WebApplicationFactory<Server.Startup> ServerWebApplicationFactory;

    public TimeWarpExecution()
    {
      var testServices = new ServiceCollection();
      ConfigureTestServices(testServices);
      ServiceProvider = testServices.BuildServiceProvider();
      ServiceScopeFactory = ServiceProvider.GetService<IServiceScopeFactory>();
    }

    public async Task Run(TestSuite aTestSuite)
    {
      IServiceScopeFactory serviceScopeFactory = ServiceProvider.GetService<IServiceScopeFactory>();
      foreach (TestClass testClass in aTestSuite.TestClasses)
      {
        Console.WriteLine($"==== Executing Cases for the class {testClass.Type.FullName} ====");
        foreach (Test test in testClass.Tests)
        {
          if (test.Has<SkipAttribute>(out SkipAttribute skip))
          {
            await test.Skip(skip.Reason);
            continue;
          }
          using IServiceScope serviceScope = serviceScopeFactory.CreateScope();
          object instance = serviceScope.ServiceProvider.GetService(testClass.Type);

          await Setup(instance, testClass);

          Console.WriteLine($"==== Execute test: {test.Name} ====");
          await test.Run(instance);

          await Cleanup(instance, testClass);
        }
      }
      (serviceScopeFactory as IDisposable).Dispose();
    }

    private async Task Setup(object aInstance, TestClass aTestClass)
    {
      Guard.Argument(aInstance, nameof(aInstance)).NotNull();

      MethodInfo methodInfo = aTestClass.Type.GetMethod(nameof(Setup));
      if (methodInfo != null)
      {
        Console.WriteLine($"==== Run Setup for class: {aTestClass.Type.Name} ====");
        await methodInfo.Call(aInstance);
      }
    }
    private async Task Cleanup(object aInstance, TestClass aTestClass)
    {
      Guard.Argument(aInstance, nameof(aInstance)).NotNull();

      MethodInfo methodInfo = aTestClass.Type.GetMethod(nameof(Cleanup));
      if (methodInfo != null)
      {
        Console.WriteLine($"==== Run CleanUp for class: {aTestClass.Type.Name} ====");
        await methodInfo.Call(aInstance);
      }
    }

    private void ConfigureTestServices(ServiceCollection aServiceCollection)
    {
      Console.WriteLine($"==== {nameof(ConfigureTestServices)} ====");
      ConfigurationApplications(aServiceCollection);

      //ServerWebApplicationFactory = new WebApplicationFactory<Server.Startup>();
      //ServerHttpClient = ServerWebApplicationFactory.CreateClient();

      //ConfigureWebAssemblyHost(aServiceCollection);

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

    //private void ConfigureWebAssemblyHost(ServiceCollection aServiceCollection)
    //{
    //  var clientHostBuilder = ClientHostBuilder.CreateDefault();
    //  //ConfigureServices(clientHostBuilder.Services);

    //  ClientHost clientHost = clientHostBuilder.Build();
    //  aServiceCollection.AddSingleton(clientHost);
    //}

    //private void ConfigureServices(IServiceCollection aServiceCollection)
    //{
    //  // Maybe call Program.ConfigureServices
    //  //Program.ConfigureServices(aServiceCollection);

    //  //// Need an HttpClient to talk to the Server side configured before calling AddBlazorState.
    //  //aServiceCollection.AddSingleton<HttpClient>();
    //  aServiceCollection.AddBlazorState
    //  (
    //    aOptions => aOptions.Assemblies =
    //    new Assembly[] { typeof(TimeWarp.Blazor.Client.Program).GetTypeInfo().Assembly }
    //  );

    //  //aServiceCollection.AddSingleton
    //  //(
    //  //  new JsonSerializerOptions
    //  //  {
    //  //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //  //  }
    //  //);

    //  aServiceCollection.AddSingleton<IClientLoaderConfiguration, ClientLoaderTestConfiguration>();
    //}

    private void ConfigurationApplications(ServiceCollection aServiceCollection)
    {
      // Maybe we add the ClientHost here and have it set up BlazorState Etc on construction?
      aServiceCollection.AddSingleton<ClientHost>();
      aServiceCollection.AddSingleton<TimeWarpBlazorServerApplication>();
      ; // Add other applications you want to run here
    }
  }
}
