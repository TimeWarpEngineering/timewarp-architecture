#region Purpose
// `dev publish compose`: run `aspire publish` for the Docker Compose environment, then gate the output
// with aspire-tests' production-safety suite. Same command locally and in CI.
#endregion

#region Design
// The publish + check pipeline is shared with `dev publish kubernetes` (services/aspire-publish.cs);
// this command names the target (Publish:Target=compose), its output directory and its safety suite
// (aspire-tests ComposePublish_Given_, pointed at the CLI output via TIMEWARP_COMPOSE_OUTPUT).
// Building/running the stack is the operator's `aspire deploy` / `aspire do prepare-compose`, which
// honour ASPIRE_CONTAINER_RUNTIME (task 277); nothing here calls a container CLI.
#endregion

namespace DevCli.Commands;

[NuruRoute("compose", Description = "aspire publish the Docker Compose target to artifacts/aspire-output/compose and check it for production safety")]
[NuruRouteExample("publish compose", Description = "Generate docker-compose.yaml + .env + migration script, then run the safety checks")]
internal sealed class PublishComposeCommand : PublishGroup, ICommand<Unit>
{
  internal static readonly AspirePublishTarget Target = new(
    Name: "compose",
    OutputDirectory: "artifacts/aspire-output/compose",
    SafetySuiteClass: "ComposePublish_Given_",
    OutputEnvironmentVariable: "TIMEWARP_COMPOSE_OUTPUT");

  internal sealed class Handler : ICommandHandler<PublishComposeCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(PublishComposeCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      string? outputDirectory = await AspirePublish.PublishAndCheckAsync(Terminal, Target, ct);
      if (outputDirectory is not null)
      {
        Terminal.WriteLine($"\nCompose publish output is production-safe: {outputDirectory}".Green());
      }

      return Unit.Value;
    }
  }
}
