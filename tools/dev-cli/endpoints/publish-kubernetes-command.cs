#region Purpose
// `dev publish kubernetes`: run `aspire publish` for the Kubernetes environment (a Helm chart), gate
// the chart with aspire-tests' production-safety suite, then `helm lint` it when Helm is installed.
#endregion

#region Design
// Same shared pipeline as `dev publish compose` (services/aspire-publish.cs), with
// Publish:Target=kubernetes and the KubernetesPublish_Given_ suite (TIMEWARP_HELM_OUTPUT) — task 070-004.
// helm lint is opportunistic: GitHub's ubuntu runners ship Helm, so CI always lints; a machine
// without helm on PATH skips the lint with a notice rather than failing, because the safety suite
// (not the linter) is the gate. Nothing here talks to a cluster: `aspire deploy` (helm upgrade
// --install against the current kubectl context) stays the operator's step.
#endregion

namespace DevCli.Commands;

[NuruRoute("kubernetes", Description = "aspire publish the Kubernetes target (Helm chart) to artifacts/aspire-output/kubernetes, check it for production safety and helm lint it")]
[NuruRouteExample("publish kubernetes", Description = "Generate the Helm chart + migration script, run the safety checks, then helm lint (when helm is installed)")]
internal sealed class PublishKubernetesCommand : PublishGroup, ICommand<Unit>
{
  internal static readonly AspirePublishTarget Target = new(
    Name: "kubernetes",
    OutputDirectory: "artifacts/aspire-output/kubernetes",
    SafetySuiteClass: "KubernetesPublish_Given_",
    OutputEnvironmentVariable: "TIMEWARP_HELM_OUTPUT");

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

      string? outputDirectory = await AspirePublish.PublishAndCheckAsync(Terminal, Target, ct);
      if (outputDirectory is null) return Unit.Value;

      if (!IsOnPath("helm"))
      {
        Terminal.WriteLine("\nhelm not found on PATH — skipping helm lint (the safety checks passed).".Yellow());
      }
      else
      {
        Terminal.WriteLine("\nhelm lint...");
        int lintExitCode = await Shell.Builder("helm")
          .WithArguments("lint", outputDirectory)
          .WithNoValidation()
          .RunAsync(ct);

        if (lintExitCode != 0)
        {
          AspirePublish.Fail(Terminal, $"helm lint failed (exit {lintExitCode}).");
          return Unit.Value;
        }
      }

      Terminal.WriteLine($"\nHelm chart is production-safe: {outputDirectory}".Green());
      return Unit.Value;
    }

    private static bool IsOnPath(string executable) =>
      (Environment.GetEnvironmentVariable("PATH") ?? "")
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Any(directory => File.Exists(Path.Combine(directory, executable)) || File.Exists(Path.Combine(directory, executable + ".exe")));
  }
}
