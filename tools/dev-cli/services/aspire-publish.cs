#region Purpose
// Shared `dev publish` pipeline: `aspire publish` one or more AppHost publish targets, then gate the
// outputs with their aspire-tests production-safety suites in one test run.
#endregion

#region Design
// The AppHost can publish only one compute environment per model, so each target is its own
// `aspire publish` call with the target passed through as `-- --Publish:Target=<name>`.
//   1. `aspire publish --apphost <csproj> --output-path <dir> --non-interactive -- --Publish:Target=<name>`
//      per target — the real CLI path an operator uses, so a publish-pipeline error fails here. Each
//      output directory is wiped first so a stale file can never pass the check.
//   2. ONE aspire-tests `dotnet test` (task 367) with every target's <OutputEnvironmentVariable>
//      pointing at its directory, so the facts inspect exactly the files CI uploads instead of their
//      own in-proc publish. One run builds and starts the test host once instead of once per target.
//      The rules live once, in the test suite; this runner only points it at the CLI output.
//      Arguments (filter, --no-build) come from AspirePublishTargets.
// helm lint is opportunistic: GitHub's ubuntu runners ship Helm, so CI always lints; a machine
// without helm on PATH skips the lint with a notice rather than failing, because the safety suite
// (not the linter) is the gate. It runs after the safety suite passes, as before task 367.
// Runtime neutrality (task 277): `aspire publish` only writes files — no image build, no deploy —
// and nothing here calls a container CLI or a cluster.
// Generated output is CI-only (never committed): artifacts/ is git-ignored; workflow.yml uploads it.
#endregion

namespace DevCli.Services;

/// <summary>Runs <c>aspire publish</c> for publish targets and their production-safety suites.</summary>
internal static class AspirePublish
{
  internal const string SafetySuiteDirectory = "tests/container-apps/aspire/aspire-tests";
  private static readonly Version MinimumCliVersion = new(13, 6);

  /// <summary>
  /// Publishes every target, then checks them all in one safety-suite run. Returns the output
  /// directories in target order, or null after reporting a failure.
  /// </summary>
  internal static async Task<IReadOnlyList<string>?> PublishAndCheckAsync(
    ITerminal terminal,
    IReadOnlyList<AspirePublishTarget> targets,
    bool noBuild,
    CancellationToken cancellationToken)
  {
    string? repoRoot = Git.FindRoot();
    if (repoRoot is null)
    {
      return Fail(terminal, "Error: could not find repository root.");
    }

    string command = $"dev publish {(targets.Count == 1 ? targets[0].Name : "all")}";
    string? versionError = await AspireCli.ValidateVersionAsync(repoRoot, MinimumCliVersion, command, cancellationToken);
    if (versionError is not null)
    {
      return Fail(terminal, $"Error: {versionError}");
    }

    List<string> outputDirectories = [];
    foreach (AspirePublishTarget target in targets)
    {
      string outputDirectory = Path.Combine(repoRoot, target.OutputDirectory);
      if (!await PublishAsync(terminal, repoRoot, target, outputDirectory, cancellationToken))
      {
        return null;
      }

      outputDirectories.Add(outputDirectory);
    }

    string names = string.Join(", ", targets.Select(target => target.Name));
    terminal.WriteLine($"\nChecking {names} output for production safety...");
    ShellBuilder check = Shell.Builder("dotnet")
      .WithArguments(AspirePublishTargets.SafetySuiteTestArguments(targets, noBuild))
      .WithWorkingDirectory(Path.Combine(repoRoot, SafetySuiteDirectory))
      .WithNoValidation();
    for (int index = 0; index < targets.Count; index++)
    {
      check = check.WithEnvironmentVariable(targets[index].OutputEnvironmentVariable, outputDirectories[index]);
    }

    int checkExitCode = await check.RunAsync(cancellationToken);

    return checkExitCode == 0
      ? outputDirectories
      : Fail(terminal, $"{names} publish output failed the production-safety checks.");
  }

  private static async Task<bool> PublishAsync(
    ITerminal terminal,
    string repoRoot,
    AspirePublishTarget target,
    string outputDirectory,
    CancellationToken cancellationToken)
  {
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

    if (publishExitCode == 0) return true;

    Fail(terminal, $"aspire publish ({target.Name}) failed (exit {publishExitCode}).");
    return false;
  }

  /// <summary>Runs <c>helm lint</c> on the chart when Helm is installed; false after reporting a lint failure.</summary>
  internal static async Task<bool> HelmLintAsync(ITerminal terminal, string chartDirectory, CancellationToken cancellationToken)
  {
    if (!IsOnPath("helm"))
    {
      terminal.WriteLine("\nhelm not found on PATH — skipping helm lint (the safety checks passed).".Yellow());
      return true;
    }

    terminal.WriteLine("\nhelm lint...");
    int lintExitCode = await Shell.Builder("helm")
      .WithArguments("lint", chartDirectory)
      .WithNoValidation()
      .RunAsync(cancellationToken);

    if (lintExitCode == 0) return true;

    Fail(terminal, $"helm lint failed (exit {lintExitCode}).");
    return false;
  }

  /// <summary>Reports <paramref name="message"/> as an error and sets a failing exit code.</summary>
  internal static IReadOnlyList<string>? Fail(ITerminal terminal, string message)
  {
    terminal.WriteErrorLine(message.Red());
    Environment.ExitCode = 1;
    return null;
  }

  private static bool IsOnPath(string executable) =>
    (Environment.GetEnvironmentVariable("PATH") ?? "")
      .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
      .Any(directory => File.Exists(Path.Combine(directory, executable)) || File.Exists(Path.Combine(directory, executable + ".exe")));
}
