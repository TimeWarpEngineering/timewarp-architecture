#region Purpose
// IPackableProjectService for the template repository: the source/ packages and the template pack.
#endregion
#region Design
// Template-only (task 533): .template.config/template.json excludes this file, and dev.cs registers
// it inside a templateRepository region; a generated app keeps TimeWarp.Nuru.DevCli's
// PackableProjectService. The DevCli scan reads source/ only, so it misses
// timewarp-templates/.../timewarp-architecture-template.csproj (TimeWarp.Architecture), and
// check-version and dev release left the template out. ReleasePackages.CandidateProjectPaths adds it.
// Evaluation is the DevCli one: PackableProjectService.EvaluateProjectsAsync runs
// `dotnet msbuild -getProperty:IsPackable,PackageId` per project in parallel and ValidateDerivedSet
// sorts by PackageId and refuses a duplicate. One pass over every candidate, so the template costs no
// extra wall time. The evaluate delegate repeats the DevCli argument list (its own is private).
#endregion

namespace DevCli.Services;

/// <summary>The 12-package set: every IsPackable project under source/ plus TimeWarp.Architecture.</summary>
internal sealed class ArchitecturePackableProjectService : IPackableProjectService
{
  private readonly ITerminal Terminal;

  public ArchitecturePackableProjectService(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
  }

  public ValueTask<IReadOnlyList<PackableProject>> GetPackableProjectsAsync(string repoRoot, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(repoRoot);

    return PackableProjectService.EvaluateProjectsAsync(
      ReleasePackages.CandidateProjectPaths(repoRoot),
      EvaluateAsync,
      line => Terminal.WriteErrorLine(line),
      cancellationToken);
  }

  private static Task<CommandOutput> EvaluateAsync(string projectPath, CancellationToken cancellationToken) =>
    DotNet.Builder()
      .WithArguments("msbuild", projectPath, "-nologo", "-nodeReuse:false", "-getProperty:IsPackable,PackageId")
      .WithNoValidation()
      .CaptureAsync(cancellationToken);
}
