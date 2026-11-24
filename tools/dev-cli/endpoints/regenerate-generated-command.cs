#region Purpose
// Rewrites the checked-in generated files: the feature filename grammar outputs and the ingress
// prefix golden.
#endregion

#region Design
// First writes the feature filename grammar outputs (FeatureFilenameGrammarGenerator) from
// feature-filename-grammar.json. A generated app has no source/analyzers/, so a missing JSON is a
// skip, not a failure. A registry that breaks a grammar rule fails the command and writes nothing.
// The grammar runs first so the host builds below compile against the fresh registry.
// Then builds yarp and the Aspire AppHost in Release, reads each WebServerApiRoutePrefixes.g.cs, and
// writes goldens/ingress only when those emits match. `dev workflow` runs this command after the fast
// test group and fails on any diff or untracked file under goldens/ingress or the grammar outputs.
// A dirty diff means a file was hand-edited, or the grammar JSON or a web-contracts [ApiRoute]
// changed without a regenerate: re-run this command and commit, or fix the generator if the new
// text is wrong.
// The Offerable offer-record generator is not rebuilt here. Task 282 removed it.
#endregion

namespace DevCli.Commands;

[NuruRoute("regenerate-generated", Description = "Rewrite the checked-in feature filename grammar files and ingress prefix golden")]
internal sealed class RegenerateGeneratedCommand : ICommand<Unit>
{
  internal sealed class Handler : IRequestHandler<RegenerateGeneratedCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(RegenerateGeneratedCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;
      if (!RegenerateGrammar()) return Unit.Value;
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

    private bool RegenerateGrammar()
    {
      string jsonPath = Path.Combine(RepoRoot, FeatureFilenameGrammarGenerator.JsonRelativePath);
      if (!File.Exists(jsonPath))
      {
        Terminal.WriteLine($"Skipping feature filename grammar: {FeatureFilenameGrammarGenerator.JsonRelativePath} not found.");
        return true;
      }

      Terminal.WriteLine("Regenerating feature filename grammar...");
      if (!FeatureFilenameGrammarGenerator.TryGenerate(
            File.ReadAllText(jsonPath),
            out IReadOnlyList<FeatureFilenameGrammarOutput> outputs,
            out string error))
      {
        Terminal.WriteErrorLine($"{FeatureFilenameGrammarGenerator.JsonRelativePath}: {error}".Red());
        Environment.ExitCode = 1;
        return false;
      }

      FeatureFilenameGrammarGenerator.Write(RepoRoot, outputs);
      foreach (FeatureFilenameGrammarOutput output in outputs)
      {
        Terminal.WriteLine($"Wrote {output.RelativePath}".Green());
      }

      return true;
    }

    private async Task<bool> RegenerateAsync()
    {
      Terminal.WriteLine("\nRegenerating ingress prefix golden...");
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
