namespace TimeWarp.Blazor.Integration.Tests.Infrastructure.Server
{
  using Fixie;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Microsoft.Extensions.DependencyInjection;
  using System.Text.Json;
  using TimeWarp.Blazor.Server;

  public class TestingConvention : Discovery, Execution
  {
    const string TestPostfix = "Tests";
    private readonly IServiceScopeFactory ServiceScopeFactory;

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
      aServiceCollection.AddSingleton(new WebApplicationFactory<Startup>());
      aServiceCollection.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
      aServiceCollection.Scan
      (
        aTypeSourceSelector => aTypeSourceSelector        // Start with all non abstract types in this assembly
          .FromAssemblyOf<TestingConvention>()
          .AddClasses(action: (aClasses) => aClasses.TypeName().EndsWith(TestPostfix))
          .AsSelf()
          .WithScopedLifetime()
      );
    }
  }
}
