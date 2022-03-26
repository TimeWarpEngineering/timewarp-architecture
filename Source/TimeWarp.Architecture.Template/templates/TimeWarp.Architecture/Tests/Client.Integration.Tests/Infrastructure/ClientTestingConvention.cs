namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure
{
  using Fixie;
  using Microsoft.Extensions.DependencyInjection;
  using System.Collections.Generic;
  using TimeWarp.Architecture.Testing;

  [NotTest]
  public class ClientTestConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment)
    {
      var testDiscovery = new TestDiscovery(aTestEnvironment.CustomArguments);
      var testExecution = new TimeWarpExecution(aTestEnvironment.CustomArguments);
      aTestConfiguration.Conventions.Add(testDiscovery, testExecution);
    }
  }

  [NotTest]
  public class TimeWarpExecution : TestExecution
  {
    public TimeWarpExecution(IReadOnlyList<string> aCustomArguments) : base(aCustomArguments) { }

    public override void ConfigureApplications(ServiceCollection aServiceCollection)
    {
      aServiceCollection.AddSingleton<TestClientApplication>();
      base.ConfigureApplications(aServiceCollection);
    }
  }
}
