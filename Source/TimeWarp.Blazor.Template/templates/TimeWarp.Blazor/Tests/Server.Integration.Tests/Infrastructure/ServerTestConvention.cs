namespace TimeWarp.Blazor.Server.Integration.Tests.Infrastructure
{
  using Fixie;
  using TimeWarp.Blazor.Testing;

  [NotTest]
  public class ServerTestConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment)
    {
      var testDiscovery = new TestDiscovery(aTestEnvironment.CustomArguments);
      var testExecution = new TestExecution(aTestEnvironment.CustomArguments);

      aTestConfiguration.Conventions.Add(testDiscovery, testExecution);
    }
  }
}
