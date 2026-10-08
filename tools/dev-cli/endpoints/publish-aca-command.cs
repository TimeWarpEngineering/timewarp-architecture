#region Purpose
// `dev publish aca`: run `aspire publish` for the Azure Container Apps environment (Bicep), then gate
// the output with aspire-tests' production-safety suite. Same command locally and in CI.
#endregion

#region Design
// Same shared pipeline as `dev publish compose` / `dev publish kubernetes` (services/aspire-publish.cs),
// with Publish:Target=aca and the AcaPublish_Given_ suite (TIMEWARP_ACA_OUTPUT) — task 070-007.
// Publishing Azure only writes Bicep: no `az` login, no subscription, no Azure credentials, so CI runs
// it on every PR. Provisioning is the operator's `dev deploy --target aca`; nothing here talks to Azure.
#endregion

namespace DevCli.Commands;

[NuruRoute("aca", Description = "aspire publish the Azure Container Apps target (Bicep) to artifacts/aspire-output/aca and check it for production safety")]
[NuruRouteExample("publish aca", Description = "Generate the ACA Bicep + migration script/bundle, then run the safety checks (no Azure credentials needed)")]
internal sealed class PublishAcaCommand : PublishGroup, ICommand<Unit>
{
  internal static readonly AspirePublishTarget Target = new(
    Name: "aca",
    OutputDirectory: "artifacts/aspire-output/aca",
    SafetySuiteClass: "AcaPublish_Given_",
    OutputEnvironmentVariable: "TIMEWARP_ACA_OUTPUT");

  internal sealed class Handler : ICommandHandler<PublishAcaCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(PublishAcaCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      string? outputDirectory = await AspirePublish.PublishAndCheckAsync(Terminal, Target, ct);
      if (outputDirectory is not null)
      {
        Terminal.WriteLine($"\nAzure Container Apps publish output is production-safe: {outputDirectory}".Green());
      }

      return Unit.Value;
    }
  }
}
