#region Purpose
// Runs the skill-spec lint CI runs, at the vally version pinned in this repo.
#endregion

#region Design
// `vally lint skills`, then `vally lint --eval-spec` for each skills/*/evals/eval.yaml.
// CI installs @microsoft/vally-cli from tools/dev-cli/vally-cli.version and calls this verb,
// so a local run and the skill-lint job cannot drift. Output is vally's. A missing vally,
// or a different version, exits non-zero and prints the npm install for the pin (Node.js
// required). A lint failure exits with vally's code. dev build does not call vally.
// The client-name scan runs first, so a skills-only PR (skill-lint, not ci fast) still fails
// when a tracked file names a client. dev build runs that same scan for every other PR.
#endregion

namespace DevCli.Commands;

[NuruRoute("lint-skills", Description = "Lint skills and eval specs with the pinned vally CLI")]
internal sealed class LintSkillsCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<LintSkillsCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(LintSkillsCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;
      if (!await ClientNameScan.ReportAsync(
            RepoRoot,
            message => Terminal.WriteLine(message),
            message => Terminal.WriteErrorLine(message.Red()),
            Ct))
      {
        return Unit.Value;
      }

      if (!await LintAsync()) return Unit.Value;

      Terminal.WriteLine("\nSkill specs linted successfully!".Green());
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

    private async Task<bool> LintAsync()
    {
      string? pinned = SkillSpecLint.ReadPinnedVersion(RepoRoot);
      if (pinned is null)
      {
        Terminal.WriteErrorLine(
          $"Error: {SkillSpecLint.VersionFileRelativePath} is missing or is not a version.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      if (!SkillSpecLint.IsOnPath("vally", Environment.GetEnvironmentVariable("PATH")))
      {
        Terminal.WriteErrorLine(SkillSpecLint.MissingExecutableMessage(pinned).Red());
        Environment.ExitCode = 1;
        return false;
      }

      if (!await VersionMatchesPinAsync(pinned)) return false;

      Terminal.WriteLine($"vally {pinned}");
      Terminal.WriteLine("vally lint skills");
      if (!await RunVallyAsync(SkillSpecLint.SkillLintArguments)) return false;

      foreach (string spec in SkillSpecLint.EvalSpecRelativePaths(RepoRoot))
      {
        Terminal.WriteLine($"Linting {spec}");
        if (!await RunVallyAsync(SkillSpecLint.EvalLintArguments(spec))) return false;
      }

      return true;
    }

    private async Task<bool> VersionMatchesPinAsync(string pinned)
    {
      CommandOutput version = await Shell.Builder("vally")
        .WithArguments("--version")
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      string? actual = SkillSpecLint.ParseVallyVersion(version.Stdout);
      if (version.Success && string.Equals(actual, pinned, StringComparison.Ordinal))
      {
        return true;
      }

      Terminal.WriteErrorLine(SkillSpecLint.VersionMismatchMessage(actual, pinned).Red());
      Environment.ExitCode = 1;
      return false;
    }

    private async Task<bool> RunVallyAsync(string[] arguments)
    {
      CommandOutput result = await Shell.Builder("vally")
        .WithArguments(arguments)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .PassthroughAsync(Ct);

      if (result.Success) return true;

      Environment.ExitCode = result.ExitCode == 0 ? 1 : result.ExitCode;
      return false;
    }
  }
}
