#region Purpose
// Run the Aspire AppHost (replaces Run.ps1), optionally on a named launch profile
#endregion
#region Design
// Launches the AppHost via the first-party Aspire CLI (`aspire run`), the modern
// interactive-development entry point that handles build/restore, the dashboard, and
// graceful Ctrl+C shutdown. Uses full interactive passthrough and points at the AppHost
// explicitly with --apphost so discovery is deterministic. Handler stores Ct and RepoRoot as
// fields so private methods are zero-parameter.
//
// --launch-profile / -lp forwards to `aspire run --launch-profile` (Aspire CLI 13.6+). Argument
// building, profile validation and the version guard live in services/aspire-run.cs (the
// `aspire --version` probe in services/aspire-cli.cs, shared with dev db nuke) so
// dev-cli-tests gate them without launching Aspire. With the option, dev run reads the
// AppHost's launchSettings.json and rejects an unknown name with the declared list, then checks
// `aspire --version` and refuses a pre-13.6 CLI with the update command (never drops the
// option silently). Without the option, the arguments are exactly the pre-option ones and no
// version probe runs, so any CLI version still works.
//
// Environment precedence: dev run still sets ASPNETCORE_ENVIRONMENT=Development on the aspire
// process (legacy Run.ps1 parity), but the selected launch profile's `environmentVariables`
// win over that inherited value. Verified with Aspire CLI 13.6.0 against a throwaway AppHost:
// parent ASPNETCORE_ENVIRONMENT=Development + profile Staging → AppHost saw Staging; with no
// -lp the first profile applies the same way. The inherited value only matters for a profile
// that does not set ASPNETCORE_ENVIRONMENT (both https and http set Development today).
//
// Before launch, warn when XAI__ApiKey and the web-server user secret XAI:ApiKey are both
// absent. The warning prints the setup command only — never the secret value, and never
// `dotnet user-secrets list` stdout (that stream holds every secret). A failed or timed-out
// list is still a warning. dev run starts either way.
#endregion

namespace DevCli.Commands;

[NuruRoute("run", Description = "Run the Aspire AppHost (Development environment)")]
internal sealed class RunCommand : ICommand<Unit>
{
  [Option("launch-profile", "lp", Description = "AppHost launch profile from launchSettings.json (e.g. https, http; default: first profile). Requires Aspire CLI 13.6+")]
  public string? LaunchProfile { get; set; }

  internal sealed class Handler : ICommandHandler<RunCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string? LaunchProfile;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(RunCommand command, CancellationToken ct)
    {
      Ct = ct;
      LaunchProfile = command.LaunchProfile;

      if (!FindRepoRoot()) return Unit.Value;
      if (LaunchProfile is not null)
      {
        if (!ValidateLaunchProfile()) return Unit.Value;
        if (!await ValidateCliVersionAsync()) return Unit.Value;
      }

      await WarnIfXaiKeyMissingAsync();
      await RunAsync();

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
      return true;
    }

    private bool ValidateLaunchProfile()
    {
      string launchSettings = Path.Combine(RepoRoot, AspireRun.LaunchSettingsFile);
      if (!File.Exists(launchSettings))
      {
        return Fail($"Error: --launch-profile given but {AspireRun.LaunchSettingsFile} does not exist.");
      }

      string[] profileNames = AspireRun.ReadLaunchProfileNames(File.ReadAllText(launchSettings));
      string? error = AspireRun.ValidateLaunchProfile(LaunchProfile!, profileNames);
      return error is null || Fail($"Error: {error}");
    }

    private async Task<bool> ValidateCliVersionAsync()
    {
      string? error = await AspireCli.ValidateVersionAsync(
        RepoRoot, AspireRun.MinimumLaunchProfileVersion, "--launch-profile", Ct);
      return error is null || Fail($"Error: {error}");
    }

    private bool Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
      return false;
    }

    private async Task WarnIfXaiKeyMissingAsync()
    {
      try
      {
        string? environmentValue = Environment.GetEnvironmentVariable(XaiPreflight.EnvironmentVariable);
        string project = Path.Combine(RepoRoot, XaiPreflight.WebServerProject);
        CommandOutput list = await Shell.Builder("dotnet")
          .WithArguments("user-secrets", "list", "--project", project)
          .WithWorkingDirectory(RepoRoot)
          .WithTimeout(TimeSpan.FromSeconds(20))
          .WithNoValidation()
          .CaptureAsync(Ct);

        if (XaiPreflight.HasKey(environmentValue, list.Success ? list.Stdout : null))
        {
          return;
        }

        Terminal.WriteLine(XaiPreflight.WarningLead(list.Success).Yellow());
        Terminal.WriteLine(XaiPreflight.SetupCommand.Yellow());
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        Terminal.WriteLine($"{XaiPreflight.UnreadableSecretsLead} ({exception.Message})".Yellow());
        Terminal.WriteLine(XaiPreflight.SetupCommand.Yellow());
      }
    }

    private async Task RunAsync()
    {
      string project = Path.Combine(RepoRoot, AspireRun.AppHostProject);
      string profileSuffix = LaunchProfile is null ? "" : $" (launch profile '{LaunchProfile}')";

      Terminal.WriteLine($"Running Aspire AppHost from {project}{profileSuffix}...");
      CommandOutput result = await Shell.Builder("aspire")
        .WithArguments(AspireRun.BuildRunArguments(project, LaunchProfile))
        .WithWorkingDirectory(RepoRoot)
        .WithEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development")
        .WithNoValidation()
        .PassthroughAsync(Ct);

      if (!result.Success)
      {
        Terminal.WriteErrorLine($"AppHost exited with code {result.ExitCode}.".Red());
        Environment.ExitCode = result.ExitCode;
      }
    }
  }
}
