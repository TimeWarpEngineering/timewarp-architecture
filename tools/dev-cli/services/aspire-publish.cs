#region Purpose
// Shared `dev publish <target>` pipeline: `aspire publish` one AppHost publish target, then gate the
// output with its aspire-tests production-safety suite.
#endregion

#region Design
// One runner, one publish target per call (tasks 070-003 / 070-004). The AppHost can publish only
// one compute environment per model, so the target is passed through as `-- --Publish:Target=<name>`.
//   1. `aspire publish --apphost <csproj> --output-path <dir> --non-interactive -- --Publish:Target=<name>`
//      — the real CLI path an operator uses, so a publish-pipeline error fails here. The output
//      directory is wiped first so a stale file can never pass the check.
//   2. The target's aspire-tests class with <OutputEnvironmentVariable> pointing at that directory,
//      so the facts inspect exactly the files CI uploads instead of their own in-proc publish. The
//      rules live once, in the test suite; this runner only points it at the CLI output.
// Runtime neutrality (task 277): `aspire publish` only writes files — no image build, no deploy —
// and nothing here calls a container CLI or a cluster.
// Generated output is CI-only (never committed): artifacts/ is git-ignored; workflow.yml uploads it.
#endregion

namespace DevCli.Services;

/// <summary>One AppHost publish target and the aspire-tests class that guards its output.</summary>
internal sealed record AspirePublishTarget(
  string Name,
  string OutputDirectory,
  string SafetySuiteClass,
  string OutputEnvironmentVariable);

/// <summary>Runs <c>aspire publish</c> for one target and its production-safety suite.</summary>
internal static class AspirePublish
{
  internal const string SafetySuiteDirectory = "tests/container-apps/aspire/aspire-tests";
  private static readonly Version MinimumCliVersion = new(13, 6);

  /// <summary>Publishes <paramref name="target"/> and checks it; returns the output directory, or null after reporting a failure.</summary>
  internal static async Task<string?> PublishAndCheckAsync(ITerminal terminal, AspirePublishTarget target, CancellationToken cancellationToken)
  {
    string? repoRoot = Git.FindRoot();
    if (repoRoot is null)
    {
      return Fail(terminal, "Error: could not find repository root.");
    }

    string? versionError = await AspireCli.ValidateVersionAsync(repoRoot, MinimumCliVersion, $"dev publish {target.Name}", cancellationToken);
    if (versionError is not null)
    {
      return Fail(terminal, $"Error: {versionError}");
    }

    string outputDirectory = Path.Combine(repoRoot, target.OutputDirectory);
    if (Directory.Exists(outputDirectory))
    {
      Directory.Delete(outputDirectory, recursive: true);
    }

    terminal.WriteLine($"\naspire publish ({target.Name}) → {outputDirectory}");
    int publishExitCode = await Shell.Builder("aspire")
      .WithArguments(
        "publish",
        "--apphost", Path.Combine(repoRoot, AspireRun.AppHostProject),
        "--output-path", outputDirectory,
        "--non-interactive",
        "--",
        $"--Publish:Target={target.Name}")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .RunAsync(cancellationToken);

    if (publishExitCode != 0)
    {
      return Fail(terminal, $"aspire publish failed (exit {publishExitCode}).");
    }

    terminal.WriteLine($"\nChecking {target.Name} output for production safety...");
    int checkExitCode = await Shell.Builder("dotnet")
      .WithArguments("test", "-c", "Release", "--", "--filter-class", target.SafetySuiteClass)
      .WithWorkingDirectory(Path.Combine(repoRoot, SafetySuiteDirectory))
      .WithEnvironmentVariable(target.OutputEnvironmentVariable, outputDirectory)
      .WithNoValidation()
      .RunAsync(cancellationToken);

    return checkExitCode == 0
      ? outputDirectory
      : Fail(terminal, $"{target.Name} publish output failed the production-safety checks.");
  }

  /// <summary>Reports <paramref name="message"/> as an error and sets a failing exit code.</summary>
  internal static string? Fail(ITerminal terminal, string message)
  {
    terminal.WriteErrorLine(message.Red());
    Environment.ExitCode = 1;
    return null;
  }
}
