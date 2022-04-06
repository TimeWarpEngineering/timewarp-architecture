namespace TimeWarp.Architecture.Testing;

using Dawn;
using Fixie;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Fixie allows for the configuration of a custom test execution process. This is our base implementation.
/// </summary>
/// <remarks>This convention looks for all classes that are public and do not have the <see cref="NotTest"/> attribute
/// And all methods within those classes that are not named with the value in <see cref="SetupMethodName"/> are tests
/// </remarks>
[NotTest]
public class TestExecution : IExecution
{
  private readonly ServiceProvider ServiceProvider;
  private readonly IReadOnlyList<string> CustomArguments;

  public TestExecution(IReadOnlyList<string> aCustomArguments)
  {
    var testServices = new ServiceCollection();
    ConfigureTestServices(testServices);
    ServiceProvider = testServices.BuildServiceProvider();
    CustomArguments = aCustomArguments;
  }

  /// <summary>
  /// This is required implementation of the IExecution interface
  /// </summary>
  /// <param name="aTestSuite"></param>
  /// <remarks>
  /// Each test is run in a new <see cref="IServiceScope"/> created by the registered <see cref="IServiceScopeFactory"/>
  /// For each test/method the following is executed:
  /// <see cref="Setup(object, TestClass)"/>
  /// <see cref="Run(TestSuite)"/>
  /// <see cref="Cleanup(object, TestClass)"/>
  /// </remarks>
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


        if (test.HasParameters)
        {
          IEnumerable<object[]> inputs = test.GetAll<InputAttribute>().Select(aInput => aInput.Parameters);

          foreach (object[] parameters in inputs)
          {
            Console.WriteLine($"==== Executing test: {test.Name} with inputs ====");
            await TryLifecycleMethod(instance, testClass, TestingConvention.SetupLifecycleMethodName);
            await test.Run(instance, parameters);
            await TryLifecycleMethod(instance, testClass, TestingConvention.CleanupLifecycleMethodName);
          }
        }
        else
        {
          Console.WriteLine($"==== Executing test: {test.Name} ====");
          await TryLifecycleMethod(instance, testClass, TestingConvention.SetupLifecycleMethodName);
          await test.Run(instance);
          await TryLifecycleMethod(instance, testClass, TestingConvention.CleanupLifecycleMethodName);
        }
      }
    }
    //await Task.Delay(TimeSpan.FromMinutes(5));// This will give me time to see if the webApplication responds

    await (serviceScopeFactory as IAsyncDisposable).DisposeAsync();
  }

  /// <summary>
  /// Registers all the items in the <see cref="ServiceCollection"/>
  /// </summary>
  /// <param name="aServiceCollection"></param>
  public virtual void ConfigureTestServices(ServiceCollection aServiceCollection)
  {
    Console.WriteLine($"==== {nameof(ConfigureTestServices)} ====");
    ConfigureApplications(aServiceCollection);

    // Configure any test class dependencies here.
    aServiceCollection.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    RegisterTests(aServiceCollection);
  }

  /// <summary>
  /// Add the <see cref="TestServerApplication{TStartup}">applications</see> to be running as Singletons to the ServiceCollection
  /// </summary>
  /// <param name="aServiceCollection"></param>
  public virtual void ConfigureApplications(ServiceCollection aServiceCollection)
  {
    Console.WriteLine($"==== {nameof(ConfigureApplications)} ====");
    aServiceCollection
      .AddSingleton<WebServerApplication>()
      .AddSingleton<ApiServerApplication>();

    ; // Add other applications you want to run here
  }

  /// <summary>
  /// we use a service collection to create the test classes. This method registers them by scanning the
  /// entry assembly.
  /// </summary>
  /// <remarks>This Filter uses the same one used in <see cref="TestDiscovery"/> </remarks>
  /// <param name="aServiceCollection"></param>
  private static void RegisterTests(ServiceCollection aServiceCollection)
  {
    aServiceCollection.Scan
    (
      aTypeSourceSelector => aTypeSourceSelector
        .FromEntryAssembly()
        .AddClasses(action: (aClasses) => aClasses.Where(TestDiscovery.TestClassFilter()))
        .AsSelf()
        .WithScopedLifetime()
    );
  }

  private static async Task TryLifecycleMethod(object aInstance, TestClass aTestClass, string aMethodName)
  {
    Guard.Argument(aInstance, nameof(aInstance)).NotNull();

    MethodInfo methodInfo = aTestClass.Type.GetMethod(aMethodName);
    if (methodInfo != null)
    {
      Console.WriteLine($"==== Run Lifecycle method: {aMethodName} ====");
      await methodInfo.Call(aInstance);
    }
  }
}
