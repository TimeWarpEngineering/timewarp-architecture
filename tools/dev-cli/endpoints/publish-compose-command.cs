#region Purpose
// `dev publish compose`: run `aspire publish` for the Docker Compose environment, then gate the output
// with aspire-tests' production-safety suite. Same command locally and in CI.
#endregion

#region Design
// Two steps, one source of rules (task 070-003):
//   1. `aspire publish --apphost <csproj> --output-path artifacts/aspire-output/compose
//      --non-interactive` — the real CLI path an operator uses, so a publish-pipeline error fails
//      here. The output directory is wiped first so a stale file can never pass the check.
//   2. aspire-tests' ComposePublish_Given_ class with TIMEWARP_COMPOSE_OUTPUT pointing at that
//      directory, so the facts inspect exactly the files CI uploads instead of their own in-proc
//      publish. The rules live once, in the test suite; this command only points it at the CLI output.
// Runtime neutrality (task 277): `aspire publish` only writes files — no image build, no compose
// up — and nothing here calls a container CLI. Building/running the stack is the operator's
// `aspire deploy` / `aspire do prepare-compose`, which honour ASPIRE_CONTAINER_RUNTIME.
// Generated output is CI-only (never committed): artifacts/ is git-ignored; workflow.yml uploads it.
#endregion

namespace DevCli.Commands;

[NuruRoute("compose", Description = "aspire publish the Docker Compose target to artifacts/aspire-output/compose and check it for production safety")]
[NuruRouteExample("publish compose", Description = "Generate docker-compose.yaml + .env + migration script, then run the safety checks")]
internal sealed class PublishComposeCommand : PublishGroup, ICommand<Unit>
{
  internal const string OutputDirectory = "artifacts/aspire-output/compose";
  internal const string SafetySuiteDirectory = "tests/container-apps/aspire/aspire-tests";
  internal const string SafetySuiteClass = "ComposePublish_Given_";
  internal const string OutputEnvironmentVariable = "TIMEWARP_COMPOSE_OUTPUT";
  private static readonly Version MinimumCliVersion = new(13, 6);

  internal sealed class Handler : ICommandHandler<PublishComposeCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(PublishComposeCommand command, CancellationToken ct)
    {
      Ct = ct;
      Environment.ExitCode = 0;

      string? root = Git.FindRoot();
      if (root is null)
      {
        Fail("Error: could not find repository root.");
        return Unit.Value;
      }

      RepoRoot = root;

      string? versionError = await AspireCli.ValidateVersionAsync(RepoRoot, MinimumCliVersion, "dev publish compose", Ct);
      if (versionError is not null)
      {
        Fail($"Error: {versionError}");
        return Unit.Value;
      }

      string outputDirectory = Path.Combine(RepoRoot, OutputDirectory);
      if (!await PublishAsync(outputDirectory)) return Unit.Value;
      if (!await CheckAsync(outputDirectory)) return Unit.Value;

      Terminal.WriteLine($"\nCompose publish output is production-safe: {outputDirectory}".Green());
      return Unit.Value;
    }

    private async Task<bool> PublishAsync(string outputDirectory)
    {
      if (Directory.Exists(outputDirectory))
      {
        Directory.Delete(outputDirectory, recursive: true);
      }

      Terminal.WriteLine($"\naspire publish → {outputDirectory}");
      int exitCode = await Shell.Builder("aspire")
        .WithArguments(
          "publish",
          "--apphost", Path.Combine(RepoRoot, AspireRun.AppHostProject),
          "--output-path", outputDirectory,
          "--non-interactive")
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .RunAsync(Ct);

      return exitCode == 0 || Fail($"aspire publish failed (exit {exitCode}).");
    }

    private async Task<bool> CheckAsync(string outputDirectory)
    {
      Terminal.WriteLine("\nChecking compose output for production safety...");
      int exitCode = await Shell.Builder("dotnet")
        .WithArguments("test", "-c", "Release", "--", "--filter-class", SafetySuiteClass)
        .WithWorkingDirectory(Path.Combine(RepoRoot, SafetySuiteDirectory))
        .WithEnvironmentVariable(OutputEnvironmentVariable, outputDirectory)
        .WithNoValidation()
        .RunAsync(Ct);

      return exitCode == 0 || Fail("Compose publish output failed the production-safety checks.");
    }

    private bool Fail(string message)
    {
      Terminal.WriteErrorLine(message.Red());
      Environment.ExitCode = 1;
      return false;
    }
  }
}
