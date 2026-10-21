#region Purpose
// Runs the `aspire --version` probe and applies the shared AspireRun.ValidateCliVersion guard.
#endregion

#region Design
// The process half of the version guard (Amuru), kept apart from aspire-run.cs so that file stays
// pure for dev-cli-tests. One `aspire --version` probe feeds the hard minimum and the comparison
// against Directory.Packages.props <AspireVersion>. A missing property is a refusal: without it
// the lag check would pass in silence. Callers that do not enforce the hard minimum
// (dev run without --launch-profile) still surface the lag warning.
#endregion

namespace DevCli.Services;

/// <summary>Aspire CLI version probe shared by dev verbs that need a minimum CLI.</summary>
internal static class AspireCli
{
  /// <summary>
  /// <see cref="MinimumError"/> is the hard-minimum or probe failure.
  /// <see cref="Warning"/> is a CLI older than the repo Aspire version.
  /// <see cref="RepoVersionError"/> is always fatal: the one Aspire version could not be read.
  /// </summary>
  internal readonly record struct AspireCliVersionReport(string? MinimumError, string? Warning, string? RepoVersionError)
  {
    /// <summary>The error to refuse on. The hard minimum applies only when <paramref name="enforceMinimum"/> is set.</summary>
    internal string? BlockingError(bool enforceMinimum) =>
      RepoVersionError ?? (enforceMinimum ? MinimumError : null);
  }

  /// <summary>Probe result for <paramref name="requirement"/>. The repo version is always read.</summary>
  internal static async Task<AspireCliVersionReport> ValidateVersionAsync(
    string repoRoot,
    Version minimum,
    string requirement,
    CancellationToken cancellationToken)
  {
    Version? repoVersion = AspireRun.ReadRepoAspireVersion(repoRoot);
    if (repoVersion is null)
    {
      return new(null, null, AspireRun.MissingRepoVersionMessage);
    }

    CommandOutput version = await Shell.Builder("aspire")
      .WithArguments("--version")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    if (!version.Success)
    {
      return new($"`aspire --version` failed (exit {version.ExitCode}). {AspireRun.UpdateHint}", null, null);
    }

    AspireRun.CliVersionAssessment assessment = AspireRun.AssessCliVersion(
      version.Stdout, minimum, requirement, repoVersion);
    return new(assessment.Error, assessment.Warning, null);
  }
}
