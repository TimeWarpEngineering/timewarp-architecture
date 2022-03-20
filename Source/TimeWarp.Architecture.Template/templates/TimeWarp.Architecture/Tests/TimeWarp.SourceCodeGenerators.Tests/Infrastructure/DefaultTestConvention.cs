namespace TimeWarp.SourceCodeGenerators.Testing
{
  using Fixie;
  using TimeWarp.Architecture.Testing;

  [NotTest]
  public class DefaultTestConvention : ITestProject
  {
    public void Configure(TestConfiguration aTestConfiguration, TestEnvironment aTestEnvironment)
    {
      var testDiscovery = new TestDiscovery(aTestEnvironment.CustomArguments);
      var testExecution = new TestExecution(aTestEnvironment.CustomArguments);

      aTestConfiguration.Conventions.Add(testDiscovery, testExecution);
    }
  }
}
