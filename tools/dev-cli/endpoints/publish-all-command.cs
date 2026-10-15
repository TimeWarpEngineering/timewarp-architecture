#region Purpose
// `dev publish all`: run `aspire publish` for every AppHost publish target (compose, kubernetes, aca),
// gate all three outputs with ONE aspire-tests safety-suite run, then `helm lint` the chart.
#endregion

#region Design
// Task 367: `dev workflow` used to run the three single-target commands, each with its own
// aspire-tests `dotnet test` — three builds and three test-host starts. This command publishes
// AspirePublishTargets.All first, then runs the three suites in one invocation with every
// TIMEWARP_*_OUTPUT variable set (services/aspire-publish.cs). The outputs, checks and helm lint are
// the same as running `dev publish compose`, `kubernetes` and `aca` one after another.
// --no-build is for a caller that just ran the Release solution build (`dev workflow`); without it
// the test run builds aspire-tests itself, so a developer never checks with stale binaries.
#endregion

namespace DevCli.Commands;

[NuruRoute("all", Description = "aspire publish every target (compose, kubernetes, aca) to artifacts/aspire-output/, check them for production safety in one test run and helm lint the chart")]
[NuruRouteExample("publish all", Description = "Publish all three targets, then run their safety checks once")]
internal sealed class PublishAllCommand : PublishGroup, ICommand<Unit>
{
  [Option("no-build", Description = "Skip build/restore of aspire-tests (the Release solution build already built it)")]
  public bool NoBuild { get; set; }

  internal sealed class Handler : ICommandHandler<PublishAllCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(PublishAllCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      IReadOnlyList<string>? outputDirectories =
        await AspirePublish.PublishAndCheckAsync(Terminal, AspirePublishTargets.All, command.NoBuild, ct);
      if (outputDirectories is null) return Unit.Value;

      string chartDirectory = outputDirectories[Array.IndexOf(AspirePublishTargets.All, AspirePublishTargets.Kubernetes)];
      if (!await AspirePublish.HelmLintAsync(Terminal, chartDirectory, ct)) return Unit.Value;

      Terminal.WriteLine("\nAll publish outputs are production-safe:".Green());
      foreach (string outputDirectory in outputDirectories)
      {
        Terminal.WriteLine($"  {outputDirectory}".Green());
      }

      return Unit.Value;
    }
  }
}
