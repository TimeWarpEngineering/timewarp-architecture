#region Purpose
// Rewrites the checked-in ingress prefix golden from the two host compilations.
#endregion

#region Design
// Builds yarp and the Aspire AppHost in Release, reads each WebServerApiRoutePrefixes.g.cs, and
// writes goldens/ingress only when those emits match. CI runs this command and then
// `git diff --exit-code -- goldens/ingress`. A dirty diff means the golden was hand-edited, or a
// web-contracts [ApiRoute] changed and the golden was not regenerated: re-run this command and
// commit, or fix the generator if the new text is wrong.
// The Offerable offer-record generator is not rebuilt here. Task 282 removed it.
#endregion

namespace DevCli.Commands;

[NuruRoute("regenerate-generated", Description = "Rewrite the checked-in ingress prefix golden from the generator")]
internal sealed class RegenerateGeneratedCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<RegenerateGeneratedCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(RegenerateGeneratedCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;
      if (!await RegenerateAsync()) return Unit.Value;

      return Unit.Value;
    }

    private bool FindRepoRoot()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return false;
      }

      RepoRoot = root;
      return true;
    }

    private async Task<bool> RegenerateAsync()
    {
      Terminal.WriteLine("Regenerating ingress prefix golden...");
      Dictionary<string, string> emits = new(StringComparer.Ordinal);

      foreach (string project in GeneratedIngressPrefixes.HostProjects)
      {
        string projectPath = Path.Combine(RepoRoot, project);
        Terminal.WriteLine($"\nBuilding {project}...");
        CommandResult build = DotNet.Build()
          .WithProject(projectPath)
          .WithConfiguration("Release")
          .WithNoValidation()
          .Build();

        int exitCode = await build.RunAsync(Ct);
        if (exitCode != 0)
        {
          Terminal.WriteErrorLine($"Build failed: {project}".Red());
          Environment.ExitCode = exitCode;
          return false;
        }

        string emitDirectory = GeneratedIngressPrefixes.ProjectEmitDirectory(RepoRoot, project);
        if (!GeneratedIngressPrefixes.TryReadEmit(emitDirectory, out string text, out string error))
        {
          Terminal.WriteErrorLine(error.Red());
          Environment.ExitCode = 1;
          return false;
        }

        emits[project] = text;
      }

      string canonical = emits[GeneratedIngressPrefixes.HostProjects[0]];
      foreach (string project in GeneratedIngressPrefixes.HostProjects.Skip(1))
      {
        if (string.Equals(emits[project], canonical, StringComparison.Ordinal))
        {
          continue;
        }

        Terminal.WriteErrorLine(
          "yarp and the Aspire AppHost emitted different WebServerApiRoutePrefixes.g.cs. The golden was not written.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      string snapshotPath = Path.Combine(RepoRoot, GeneratedIngressPrefixes.SnapshotRelativePath);
      GeneratedIngressPrefixes.WriteSnapshot(snapshotPath, canonical);
      Terminal.WriteLine($"\nWrote {GeneratedIngressPrefixes.SnapshotRelativePath}".Green());
      Terminal.WriteLine(
        "Server-offer golden: none. Task 282 removed the Offerable generator; availability flags are hand-written.");
      return true;
    }
  }
}
