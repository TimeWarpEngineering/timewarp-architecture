#region Purpose
// Build all projects in the repository
#endregion
#region Design
// Discovers the repository root dynamically using Git.FindRoot()
// Handler stores Command and Ct as fields so private methods are zero-parameter
// Captures MSBuild output via Amuru CaptureAsync by default: a green build prints one summary line,
// a failed step prints the whole captured output to stderr. --verbose streams via RunAsync.
// --quiet is the default and stays accepted. Mode rules and the summary: services/build-output.cs.
// dev workflow (CI) builds with the defaults, so a red CI build still shows every diagnostic.
// In the template repository, before MSBuild, scan tracked files for client names (the same scan
// as dev check-client-names). CI's fast job runs this build. A skills-only PR does not, so dev
// lint-skills runs the scan too. ganda repo audit has no word-list check. The scan is a
// templateRepository region (task 530): a generated app has no name list, so its build skips it.
// Task 526: after the .slnx, build each tests/ project the .slnx does not list (the Jaribu family
// aggregators and timewarp-testing-tests), one at a time like dev test's build phase so shared
// obj/ and bin/ folders never race. They stay out of the .slnx on purpose, but they compile every
// co-located runfile, so a runfile compile error used to pass dev build and fail first in CI.
// Discovery is DotNetTestArguments.TestProjects / UnlistedProjects (a disk glob, not a list), so a
// generated app with a family flagged off builds only what it has. --clean cleans them too.
// Task 535: last, `dotnet build tools/dev-cli/dev.cs -c Release` compiles the dev CLI runfile (not in
// the .slnx). A plain build never runs the NativeAOT link (only `dev self-install` publishes), so it
// takes seconds, and it still runs the AOT and trim analyzers PublishAot=true turns on, with
// warnings as errors. Its output goes to the SDK's runfile artifacts folder, never ./bin/dev, so
// building from the running ./bin/dev is safe. When dev runs as that runfile (CI, template smoke),
// the inputs it just compiled are up to date and MSBuild writes nothing; on Linux a rewrite gets
// a new inode and the running process keeps the old one. --clean cleans the runfile too.
// It is the same step in a generated app: dev.cs ships there with templateRepository stripped.
#endregion

namespace DevCli.Commands;

using System.Diagnostics;

[NuruRoute("build", Description = "Build all projects in the repository")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("verbose", "v", Description = "Stream full MSBuild output while building")]
  public bool Verbose { get; set; }

  [Option("quiet", "q", Description = "Hide MSBuild output unless the build fails (the default)")]
  public bool Quiet { get; set; }

  internal sealed class Handler : IRequestHandler<BuildCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private BuildCommand Command = null!;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private BuildOutputMode Mode;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;
      Mode = BuildOutput.Select(command.Verbose, command.Quiet);
      var stopwatch = Stopwatch.StartNew();

      if (!FindRepoRoot()) return Unit.Value;
#if templateRepository
      if (!await ClientNameScan.ReportAsync(
            RepoRoot,
            message => Terminal.WriteLine(message),
            message => Terminal.WriteErrorLine(message.Red()),
            Ct))
      {
        return Unit.Value;
      }
#endif

      if (!await CleanAsync()) return Unit.Value;
      if (!await BuildAsync()) return Unit.Value;

      Terminal.WriteLine(Mode == BuildOutputMode.Stream
        ? $"\n{BuildOutput.SuccessText}".Green()
        : BuildOutput.SuccessSummary(RepoRoot, stopwatch.Elapsed).Green());
      return Unit.Value;
    }

    private bool FindRepoRoot()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return false;
      }

      RepoRoot = root;
      Terminal.WriteLine($"Building repository at {RepoRoot}...");
      return true;
    }

    private string SolutionFile => Path.Combine(RepoRoot, "timewarp-architecture.slnx");

    private string DevCliRunfile => Path.Combine(RepoRoot, BuildOutput.DevCliRunfile);

    private async Task<bool> CleanAsync()
    {
      if (!Command.Clean) return true;

      WriteStepLine($"Cleaning {SolutionFile}...");
      CommandResult command = DotNet.Clean()
        .WithProject(SolutionFile)
        .WithNoValidation()
        .Build();

      if (!await ExecuteAsync(command, "Clean failed!")) return false;

      foreach (string project in await UnlistedProjectsAsync())
      {
        WriteStepLine($"Cleaning {Path.GetFileNameWithoutExtension(project)} (not in the solution)...");
        CommandResult clean = DotNet.Clean()
          .WithProject(project)
          .WithNoValidation()
          .Build();
        if (!await ExecuteAsync(clean, $"Clean failed: {Path.GetFileNameWithoutExtension(project)}")) return false;
      }

      WriteStepLine($"Cleaning {BuildOutput.DevCliRunfile} (dev CLI runfile)...");
      CommandResult cleanDevCli = DotNet.Clean()
        .WithProject(DevCliRunfile)
        .WithConfiguration("Release")
        .WithNoValidation()
        .Build();
      return await ExecuteAsync(cleanDevCli, $"Clean failed: {BuildOutput.DevCliRunfile}");
    }

    private async Task<bool> BuildAsync()
    {
      WriteStepLine($"Building {SolutionFile}...");
      CommandResult command = DotNet.Build()
        .WithProject(SolutionFile)
        .WithConfiguration("Release")
        .WithNoValidation()
        .Build();

      if (!await ExecuteAsync(command, "Build failed!")) return false;

      // One at a time, like dev test's build phase: these share obj/ and bin/ folders with the solution's projects.
      foreach (string project in await UnlistedProjectsAsync())
      {
        WriteStepLine($"Building {Path.GetFileNameWithoutExtension(project)} (not in the solution, Release)...");
        CommandResult build = DotNet.Build()
          .WithConfiguration("Release")
          .WithWorkingDirectory(Path.GetDirectoryName(project)!)
          .WithNoValidation()
          .Build();
        if (!await ExecuteAsync(build, $"Build failed: {Path.GetFileNameWithoutExtension(project)}")) return false;
      }

      // dotnet build, never publish: no NativeAOT link, and the output stays out of ./bin/dev.
      WriteStepLine(BuildOutput.DevCliStepText);
      CommandResult devCli = DotNet.Build()
        .WithProject(DevCliRunfile)
        .WithConfiguration("Release")
        .WithNoValidation()
        .Build();
      return await ExecuteAsync(devCli, $"Build failed: {BuildOutput.DevCliRunfile}");
    }

    private async Task<string[]> UnlistedProjectsAsync()
    {
      HashSet<string> solutionProjects =
        DotNetTestArguments.SolutionProjects(await File.ReadAllTextAsync(SolutionFile, Ct), RepoRoot);
      return DotNetTestArguments.UnlistedProjects(
        DotNetTestArguments.TestProjects(Path.Combine(RepoRoot, "tests")), solutionProjects);
    }

    // Streamed output needs a blank line between steps; captured output prints the step line alone.
    private void WriteStepLine(string message) =>
      Terminal.WriteLine(Mode == BuildOutputMode.Stream ? "\n" + message : message);

    private async Task<bool> ExecuteAsync(CommandResult command, string failureMessage)
    {
      if (Mode == BuildOutputMode.Capture)
      {
        CommandOutput result = await command.CaptureAsync(Ct);
        if (!result.Success)
        {
          Terminal.WriteErrorLine(result.Combined);
          Terminal.WriteErrorLine(failureMessage.Red());
          Environment.ExitCode = 1;
          return false;
        }

        return true;
      }

      int exitCode = await command.RunAsync(Ct);
      if (exitCode != 0)
      {
        Terminal.WriteErrorLine(failureMessage.Red());
        Environment.ExitCode = exitCode;
        return false;
      }

      return true;
    }
  }
}