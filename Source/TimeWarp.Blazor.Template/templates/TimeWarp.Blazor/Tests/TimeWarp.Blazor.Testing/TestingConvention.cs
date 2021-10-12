namespace TimeWarp.Blazor.Testing
{
  using Fixie;

  [NotTest]
  public class TestingConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment) =>
      aTestConfiguration.Conventions.Add<TestDiscovery, TestExecution>();
  }
}
