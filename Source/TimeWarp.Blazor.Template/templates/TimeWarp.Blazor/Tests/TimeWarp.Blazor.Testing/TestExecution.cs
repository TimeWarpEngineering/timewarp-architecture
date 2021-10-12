namespace TimeWarp.Blazor.Testing
{
  using Dawn;
  using Fixie;
  using Microsoft.Extensions.DependencyInjection;
  using System;
  using System.Reflection;
  using System.Text.Json;
  using System.Threading.Tasks;

  [NotTest]
  public class TestExecution : IExecution
  {
    //private readonly IServiceScopeFactory ServiceScopeFactory;
    private readonly ServiceProvider ServiceProvider;
    //private bool Disposed;

    public TestExecution()
    {
      var testServices = new ServiceCollection();
      ConfigureTestServices(testServices);
      ServiceProvider = testServices.BuildServiceProvider();
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

          Console.WriteLine($"==== Run Setup for test: {test.Name} ====");
          await Setup(instance, testClass);

          Console.WriteLine($"==== Execute test: {test.Name} ====");
          await test.Run(instance);

          Console.WriteLine($"==== Run CleanUp for test: {test.Name} ====");
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
        await methodInfo.Call(aInstance);
    }
    private async Task Cleanup(object aInstance, TestClass aTestClass)
    {
      Guard.Argument(aInstance, nameof(aInstance)).NotNull();

      MethodInfo methodInfo = aTestClass.Type.GetMethod(nameof(Cleanup));
      if (methodInfo != null)
        await methodInfo.Call(aInstance);
    }

    private void ConfigureTestServices(ServiceCollection aServiceCollection)
    {
      Console.WriteLine($"==== {nameof(ConfigureTestServices)} ====");
      ConfigurationApplications(aServiceCollection);

      aServiceCollection.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

      aServiceCollection.Scan
      (
        aTypeSourceSelector => aTypeSourceSelector
          .FromEntryAssembly()
          .AddClasses(action: (aClasses) => aClasses.Where(aType => aType.IsPublic && !aType.Has<NotTest>()))
          .AsSelf()
          .WithScopedLifetime()
      );
    }

    private void ConfigurationApplications(ServiceCollection aServiceCollection)
    {
      aServiceCollection.AddSingleton<TimeWarpBlazorServerApplication>();
      ; // Add other applications you want to run here
    }

    //public void Dispose()
    //{
    //  Dispose(true);
    //  GC.SuppressFinalize(this);
    //}

    //protected virtual void Dispose(bool aIsDisposing)
    //{
    //  if (!Disposed)
    //  {
    //    if (aIsDisposing)
    //    {
    //      Console.WriteLine("==== Disposing ServiceScopeFactory ====");
    //      //(ServiceScopeFactory as IDisposable)?.Dispose();
    //      Console.WriteLine("==== ServiceScopeFactory.Disposed ====");
    //    }
    //    Disposed = true;
    //  }
    //}

  }
}
