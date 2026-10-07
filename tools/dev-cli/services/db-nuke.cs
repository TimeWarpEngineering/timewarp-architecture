#region Purpose
// Argument builder, --yes refusal text and the pre-13.6-volume hint for `dev db nuke`.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests can Compile-include them and gate
// the destructive verb without running Aspire.
//
// Nuke wraps `aspire stop --force --volumes` and nothing else: Aspire decides which volumes it owns
// and removes them. The dev CLI does not reconstruct Aspire's volume names or sweep the container
// runtime for leftovers. A volume that predates Aspire 13.6 ownership records is adopted, not
// owned, so Aspire leaves it; the hint tells the operator how to find and remove it by hand, naming
// the container CLI from ASPIRE_CONTAINER_RUNTIME (the same variable Aspire honours).
#endregion

namespace DevCli.Services;

/// <summary>Builds the <c>aspire stop</c> invocation and the operator text for <c>dev db nuke</c>.</summary>
internal static class DbNuke
{
  /// <summary>First Aspire CLI version whose <c>aspire stop</c> accepts <c>--force --volumes</c>.</summary>
  internal static readonly Version MinimumCliVersion = new(13, 6);

  internal const string Requirement = "`dev db nuke` (aspire stop --force --volumes)";

  /// <summary>Environment variable Aspire reads to pick the container runtime CLI.</summary>
  internal const string ContainerRuntimeVariable = "ASPIRE_CONTAINER_RUNTIME";

  /// <summary>Aspire's default container runtime when <see cref="ContainerRuntimeVariable"/> is unset.</summary>
  internal const string DefaultContainerRuntime = "docker";

  internal static string[] BuildStopArguments(string appHostProject) =>
    ["stop", "--apphost", appHostProject, "--force", "--volumes", "--non-interactive", "--nologo"];

  /// <summary>What <c>dev db nuke</c> prints (and refuses) when <c>--yes</c> is absent.</summary>
  internal static string[] BuildRefusalLines(string appHostProject) =>
  [
    "Refusing to nuke without --yes. `dev db nuke --yes` stops the AppHost and deletes its Aspire-owned volumes:",
    $"  aspire {string.Join(' ', BuildStopArguments(appHostProject))}",
    "The next `dev run` starts empty: migrations re-apply and seeds re-run.",
    "To keep the volume and only wipe the schema inside a running AppHost, use `dev db reset --yes`.",
  ];

  /// <summary>Container CLI named in the hint: <paramref name="containerRuntime"/> when set, else Aspire's default.</summary>
  internal static string ContainerRuntimeCli(string? containerRuntime) =>
    string.IsNullOrWhiteSpace(containerRuntime) ? DefaultContainerRuntime : containerRuntime.Trim();

  /// <summary>Printed after <c>aspire stop</c>: how to remove a pre-13.6 volume Aspire adopted rather than owned.</summary>
  internal static string[] BuildAdoptedVolumeHintLines(string? containerRuntime)
  {
    string cli = ContainerRuntimeCli(containerRuntime);
    return
    [
      "Note: a volume created before Aspire 13.6 may survive, because Aspire adopted it rather than owning it.",
      $"To remove it by hand: `{cli} volume ls`, then `{cli} volume rm <name>`.",
    ];
  }
}
