namespace TimeWarp.Architecture.Template.Tests;
using TimeWarp.Fixie;
using Boxed.DotnetNewTest;
using Boxed.Templates.FunctionalTest;

public class TestingConvention : TimeWarp.Fixie.TestingConvention { }

public class TemplateTest
{
  private const string ShortName = "timewarp-architecture";
  private const string TemplateDll = "TimeWarp.Architecture";
  private static readonly string[] DefaultArguments = new string[]
  {  
  };

  
  [Input("default", new string[] { } )]
  public async Task RestoreAndBuild_CustomArguments_IsSuccessful(string name, params string[] arguments)
  {
      var tempDirectory = TempDirectory.NewTempDirectory();
    
      // dotnet restore.
      var project = await tempDirectory
          .DotnetNewAsync(ShortName, name, DefaultArguments.ToArguments( arguments ) )
          .ConfigureAwait(false);
      await project.DotnetRestoreWithRetryAsync().ConfigureAwait( false );
      await project.DotnetBuildAsync().ConfigureAwait( false );
      await project.DotnetTestAsync().ConfigureAwait( false );
  }

  private static Task InstallTemplateAsync() => DotnetNew.InstallAsync(TemplateDll);

  private static Task UnInstallTemplateAsync() => DotnetNew.UninstallAsync(TemplateDll);

  public async static Task Setup() =>  await InstallTemplateAsync();
  public async static Task Cleanup() => await UnInstallTemplateAsync();
}