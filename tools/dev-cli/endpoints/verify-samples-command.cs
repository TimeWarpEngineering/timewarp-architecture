#region Purpose
// Compiles every project and file-based app under samples/.
#endregion
#region Design
// Vacuous success when samples/ has no compilable files (the template ships only .gitkeep).
// That is the honest empty-set result — not a fake "verified successfully" around a TODO.
// A missing samples/ directory is an error because the placeholder is part of the template.
// Each *.csproj is `dotnet build -c Release`. Loose *.cs files not covered by a csproj are
// treated as .NET 10 file-based apps.
#endregion

namespace DevCli.Commands;

[NuruRoute("verify-samples", Description = "Verify code samples compile")]
internal sealed class VerifySamplesCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await VerifyAsync()) return Value;

      return Value;
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

    private async Task<bool> VerifyAsync()
    {
      string samplesDirectory = Path.Combine(RepoRoot, "samples");
      if (!Directory.Exists(samplesDirectory))
      {
        Terminal.WriteErrorLine("Error: samples/ directory is missing.");
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine("Verifying samples...");

      List<string> compilableSamples = FindCompilableSamples(samplesDirectory);
      if (compilableSamples.Count == 0)
      {
        Terminal.WriteLine("No compilable samples under samples/; nothing to verify.");
        return true;
      }

      foreach (string samplePath in compilableSamples)
      {
        string relativePath = Path.GetRelativePath(RepoRoot, samplePath);
        Terminal.WriteLine($"\nBuilding {relativePath}...");
        CommandResult command = DotNet.Build()
          .WithProject(samplePath)
          .WithConfiguration("Release")
          .WithNoValidation()
          .Build();

        int exitCode = await command.RunAsync(Ct);
        if (exitCode != 0)
        {
          Terminal.WriteErrorLine($"Sample failed to compile: {relativePath}".Red());
          Environment.ExitCode = exitCode;
          return false;
        }
      }

      Terminal.WriteLine("\nSamples verified successfully!".Green());
      return true;
    }

    private static List<string> FindCompilableSamples(string samplesDirectory)
    {
      string[] projects = Directory
        .GetFiles(samplesDirectory, "*.csproj", SearchOption.AllDirectories)
        .Where(NotInBinOrObj)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

      string[] looseCsharpFiles = Directory
        .GetFiles(samplesDirectory, "*.cs", SearchOption.AllDirectories)
        .Where(NotInBinOrObj)
        .Where(path => !IsCoveredByProject(path, projects))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

      List<string> compilableSamples = [.. projects, .. looseCsharpFiles];
      return compilableSamples;
    }

    private static bool NotInBinOrObj(string path) =>
      !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
      && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static bool IsCoveredByProject(string csharpPath, string[] projects)
    {
      string? csharpDirectory = Path.GetDirectoryName(csharpPath);
      if (csharpDirectory is null) return false;

      foreach (string project in projects)
      {
        string? projectDirectory = Path.GetDirectoryName(project);
        if (projectDirectory is null) continue;

        if (csharpDirectory.Equals(projectDirectory, StringComparison.OrdinalIgnoreCase))
          return true;

        string prefix = projectDirectory.EndsWith(Path.DirectorySeparatorChar)
          ? projectDirectory
          : projectDirectory + Path.DirectorySeparatorChar;
        if (csharpDirectory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          return true;
      }

      return false;
    }
  }
}
