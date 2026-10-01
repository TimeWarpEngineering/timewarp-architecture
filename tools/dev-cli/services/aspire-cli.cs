#region Purpose
// Runs the `aspire --version` probe and applies the shared AspireRun.ValidateCliVersion guard.
#endregion

#region Design
// The process half of the version guard (Amuru), kept apart from aspire-run.cs so that file stays
// pure for dev-cli-tests. dev run -lp and dev db nuke both call this: one probe, one guard, one
// update hint.
#endregion

namespace DevCli.Services;

/// <summary>Aspire CLI version probe shared by dev verbs that need a minimum CLI.</summary>
internal static class AspireCli
{
  /// <summary>Error message when the installed <c>aspire</c> cannot run <paramref name="requirement"/>; null when it can.</summary>
  internal static async Task<string?> ValidateVersionAsync(
    string repoRoot,
    Version minimum,
    string requirement,
    CancellationToken cancellationToken)
  {
    CommandOutput version = await Shell.Builder("aspire")
      .WithArguments("--version")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    if (!version.Success)
    {
      return $"`aspire --version` failed (exit {version.ExitCode}). {AspireRun.UpdateHint}";
    }

    return AspireRun.ValidateCliVersion(version.Stdout, minimum, requirement);
  }
}
