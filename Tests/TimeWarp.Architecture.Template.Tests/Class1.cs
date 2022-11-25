namespace TimeWarp.Architecture.Template.Tests;
using FluentAssertions;
using TimeWarp.Fixie;
using Boxed.DotnetNewTest;
using Boxed.AspNetCore;
using Boxed.Templates.FunctionalTest;

public class Class1 : TimeWarp.Fixie.TestingConvention { }

[TestTag( TestTags.Fast )]
public class SimpleNoApplicationTest_Should_
{
  public static void AlwaysPass() => true.Should().BeTrue();

  [Skip( "Demonstrates skip attribute" )]
  public static void SkipExample() => true.Should().BeFalse();

  [TestTag( TestTags.Fast )]
  public static void TagExample() => true.Should().BeTrue();

  [Input( 5, 3, 2 )]
  [Input( 8, 5, 3 )]
  public static void Subtract(int aX, int aY, int aExpectedDifference)
  {
    int result = aX - aY;
    result.Should().Be(aExpectedDifference);
  }
}


public class ApiTemplateTest
{
  private const string TemplateName = "api";
  private const string SolutionFileName = "ApiTemplate.sln";
  private static readonly string[] DefaultArguments = new string[]
  {
        "no-open-todo=true",
        "https-port={HTTPS_PORT}",
        "http-port={HTTP_PORT}",
  };

  [Input( "StatusEndpointOn", "status-endpoint=true" )]
  [Input( "StatusEndpointOff", "status-endpoint=false" )]
  public async Task RestoreAndBuild_CustomArguments_IsSuccessful(string name, params string[] arguments)
  {
    await InstallTemplateAsync().ConfigureAwait( false );
    await using (var tempDirectory = TempDirectory.NewTempDirectory())
    {
      var project = await tempDirectory
          .DotnetNewAsync( TemplateName, name, DefaultArguments.ToArguments( arguments ) )
          .ConfigureAwait( false );
      await project.DotnetRestoreWithRetryAsync().ConfigureAwait( false );
      await project.DotnetBuildAsync().ConfigureAwait( false );
      await project.DotnetTestAsync().ConfigureAwait( false );
    }
  }

  private Task InstallTemplateAsync() => DotnetNew.InstallAsync<ApiTemplateTest>("ApiTemplate.sln");
}