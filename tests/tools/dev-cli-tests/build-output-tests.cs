#region Purpose
// Pins `dev build` output rules: MSBuild output is captured by default, --verbose streams it,
// --quiet is accepted as the default, and a captured green build prints one summary line.
// Task 535: `dev build` compiles the dev CLI runfile last; its path names a real file and its step
// line says the build skips the NativeAOT link.
#endregion

namespace BuildOutput_;

public class Select_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Select_Given_>();

  public static Task NoFlags_Should_CaptureOutput()
  {
    BuildOutput.Select(verbose: false, quiet: false).ShouldBe(BuildOutputMode.Capture);
    return Task.CompletedTask;
  }

  public static Task Verbose_Should_StreamOutput()
  {
    BuildOutput.Select(verbose: true, quiet: false).ShouldBe(BuildOutputMode.Stream);
    return Task.CompletedTask;
  }

  public static Task Quiet_Should_CaptureOutputLikeTheDefault()
  {
    BuildOutput.Select(verbose: false, quiet: true).ShouldBe(BuildOutputMode.Capture);
    return Task.CompletedTask;
  }

  public static Task VerboseAndQuiet_Should_StreamOutput()
  {
    BuildOutput.Select(verbose: true, quiet: true).ShouldBe(BuildOutputMode.Stream);
    return Task.CompletedTask;
  }
}

public class SuccessSummary_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SuccessSummary_Given_>();

  public static Task RootAndElapsed_Should_PrintOneLineWithSecondsAndRoot()
  {
    string line = BuildOutput.SuccessSummary("/repo", TimeSpan.FromMilliseconds(46_812));

    line.ShouldBe("Build completed successfully! (46.8s, /repo)");
    line.ShouldNotContain('\n');
    return Task.CompletedTask;
  }
}

public class DevCliRunfile_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DevCliRunfile_Given_>();

  public static Task RepositoryRoot_Should_NameTheDevCliEntryPoint()
  {
    string path = Path.Combine(RepoRoot(), BuildOutput.DevCliRunfile);

    File.Exists(path).ShouldBeTrue(path);
    File.ReadAllText(path).ShouldContain("NuruApp.CreateBuilder()");
    return Task.CompletedTask;
  }

  public static Task StepText_Should_NameTheRunfileAndSkipTheAotLink()
  {
    BuildOutput.DevCliStepText.ShouldContain(BuildOutput.DevCliRunfile);
    BuildOutput.DevCliStepText.ShouldContain("no NativeAOT link");
    BuildOutput.DevCliStepText.ShouldNotContain('\n');
    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
