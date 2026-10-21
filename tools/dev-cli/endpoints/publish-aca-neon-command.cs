#region Purpose
// `dev publish aca-neon`: run `aspire publish` for Azure Container Apps with external Neon Postgres,
// then gate the output with aspire-tests' production-safety suite.
#endregion

#region Design
// Same shared pipeline as `dev publish aca` (services/aspire-publish.cs), with
// AspirePublishTargets.AcaNeon: Publish:Target=aca-neon and the AcaNeonPublish_Given_ suite
// (TIMEWARP_ACA_NEON_OUTPUT) — task 317. Publishing only writes Bicep. No `az` login, no Neon
// connection string and no Azure credential is required, so CI runs it with the other targets.
// Provisioning is the operator's `dev deploy --target aca-neon`.
#endregion

namespace DevCli.Commands;

[NuruRoute("aca-neon", Description = "aspire publish the Azure Container Apps + Neon target (Bicep) to artifacts/aspire-output/aca-neon and check it for production safety")]
[NuruRouteExample("publish aca-neon", Description = "Generate the ACA + Neon Bicep (no Flexible Server, min replicas 0) and run the safety checks")]
internal sealed class PublishAcaNeonCommand : PublishGroup, ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<PublishAcaNeonCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(PublishAcaNeonCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      string? outputDirectory = (await AspirePublish.PublishAndCheckAsync(Terminal, [AspirePublishTargets.AcaNeon], noBuild: false, ct))?[0];
      if (outputDirectory is not null)
      {
        Terminal.WriteLine($"\nAzure Container Apps + Neon publish output is production-safe: {outputDirectory}".Green());
      }

      return Unit.Value;
    }
  }
}
