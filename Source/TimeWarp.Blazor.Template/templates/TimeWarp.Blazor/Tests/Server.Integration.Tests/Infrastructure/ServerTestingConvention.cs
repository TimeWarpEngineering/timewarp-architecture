namespace TimeWarp.Blazor.Server.Integration.Tests.Infrastructure
{
  using Fixie;
  using TimeWarp.Blazor.Testing;

  [NotTest]
  public class ServerTestConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment) =>
      aTestConfiguration.Conventions.Add<TestDiscovery, TestExecution>();
  }
}
