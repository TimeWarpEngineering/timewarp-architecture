#region Purpose
// `dev publish kubernetes`: run `aspire publish` for the Kubernetes environment (a Helm chart), gate
// the chart with aspire-tests' production-safety suite, then `helm lint` it when Helm is installed.
#endregion

#region Design
// Same shared pipeline as `dev publish compose` (services/aspire-publish.cs), with
// AspirePublishTargets.Kubernetes: Publish:Target=kubernetes and the KubernetesPublish_Given_ suite
// (TIMEWARP_HELM_OUTPUT) — task 070-004. helm lint (AspirePublish.HelmLintAsync) is opportunistic:
// a machine without helm on PATH skips it with a notice. Nothing here talks to a cluster:
// `aspire deploy` (helm upgrade --install against the current kubectl context) stays the operator's step.
#endregion

namespace DevCli.Commands;

[NuruRoute("kubernetes", Description = "aspire publish the Kubernetes target (Helm chart) to artifacts/aspire-output/kubernetes, check it for production safety and helm lint it")]
[NuruRouteExample("publish kubernetes", Description = "Generate the Helm chart + migration script, run the safety checks, then helm lint (when helm is installed)")]
internal sealed class PublishKubernetesCommand : PublishGroup, ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<PublishKubernetesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(PublishKubernetesCommand command, CancellationToken ct)
    {
      Environment.ExitCode = 0;

      string? outputDirectory = (await AspirePublish.PublishAndCheckAsync(Terminal, [AspirePublishTargets.Kubernetes], noBuild: false, ct))?[0];
      if (outputDirectory is null) return Unit.Value;
      if (!await AspirePublish.HelmLintAsync(Terminal, outputDirectory, ct)) return Unit.Value;

      Terminal.WriteLine($"\nHelm chart is production-safe: {outputDirectory}".Green());
      return Unit.Value;
    }
  }
}
