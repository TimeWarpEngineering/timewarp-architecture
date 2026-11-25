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
// --launch-profile / -lp forwards to `aspire run --launch-profile`. Argument building, profile
// validation and the version guard live in services/aspire-run.cs (the `aspire --version` probe
// in services/aspire-cli.cs, shared with dev db nuke) so dev-cli-tests gate them without
// launching Aspire. With the option, dev run reads the AppHost's launchSettings.json and rejects
// an unknown name with the declared list. Every dev run refuses a CLI below
// AspireRun.MinimumRunVersion (17.0) with the update command: since Aspire 17 (task 498) the
// AppHost takes DCP and the dashboard from the Aspire CLI bundle (ASPIRE013). Every dev run also
// warns when the installed CLI is older than <AspireVersion> in Directory.Packages.props and
// prints `dotnet tool update -g Aspire.Cli`.
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
  [Option("launch-profile", "lp", Description = "AppHost launch profile from launchSettings.json (e.g. https, http; default: first profile). Requires Aspire CLI 17.0+")]
  public string? LaunchProfile { get; set; }

  internal sealed class Handler : IRequestHandler<RunCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string? LaunchProfile;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(RunCommand command, CancellationToken ct)
    {
      Ct = ct;
      LaunchProfile = command.LaunchProfile;

      if (!FindRepoRoot()) return Unit.Value;
      if (LaunchProfile is not null && !ValidateLaunchProfile()) return Unit.Value;
      if (!await CheckCliVersionAsync()) return Unit.Value;

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

    private async Task<bool> CheckCliVersionAsync()
    {
      AspireCli.AspireCliVersionReport report = await AspireCli.ValidateVersionAsync(
        RepoRoot, AspireRun.MinimumRunVersion, AspireRun.RunRequirement, Ct);
      string? error = report.BlockingError(enforceMinimum: true);
      if (error is not null)
      {
        return Fail($"Error: {error}");
      }

      if (report.Warning is not null)
      {
        Terminal.WriteLine(report.Warning.Yellow());
      }

      return true;
    }

    private bool Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
      return false;
    }

    // DotNetUserSecretsListBuilder has no WithTimeout, so a linked token bounds the list. Its
    // cancellation is the timeout warning unless dev run itself was cancelled.
    private static readonly TimeSpan UserSecretsListTimeout = TimeSpan.FromSeconds(20);

    private async Task WarnIfXaiKeyMissingAsync()
    {
      // An app generated with --web false has no web-server and no Ctrl-K Ask (task 530).
      if (!AppFeatures.IsPresent(RepoRoot, AppFeatures.Web)) return;

      try
      {
        string? environmentValue = Environment.GetEnvironmentVariable(XaiPreflight.EnvironmentVariable);
        string project = Path.Combine(RepoRoot, XaiPreflight.WebServerProject);
        using var listTimeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        listTimeout.CancelAfter(UserSecretsListTimeout);
        CommandOutput list = await DotNet.UserSecrets()
          .WithProject(project)
          .WithWorkingDirectory(RepoRoot)
          .List()
          .WithNoValidation()
          .CaptureAsync(listTimeout.Token);

        if (XaiPreflight.HasKey(environmentValue, list.Success ? list.Stdout : null))
        {
          return;
        }

        Terminal.WriteLine(XaiPreflight.WarningLead(list.Success).Yellow());
        Terminal.WriteLine(XaiPreflight.SetupCommand.Yellow());
      }
      catch (Exception exception) when (exception is not OperationCanceledException || !Ct.IsCancellationRequested)
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
