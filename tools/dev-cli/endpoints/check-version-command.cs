#region Purpose
// Check if the source version matches the git tag
#endregion
#region Design
// Delegates to the ganda CLI to perform the check
#endregion

namespace DevCli.Commands;

[NuruRoute("check-version", Description = "Verify version matches git tag")]
internal sealed class CheckVersionCommand : ICommand<Unit>
{
  [Option("tag", Description = "Git tag to verify against (defaults to GITHUB_REF_NAME or git describe)")]
  public string? Tag { get; set; }

  internal sealed class Handler : ICommandHandler<CheckVersionCommand, Unit>
  {
    public async ValueTask<Unit> Handle(CheckVersionCommand command, CancellationToken ct)
    {
      ArgumentNullException.ThrowIfNull(command);

      List<string> args = ["repo", "check-version", "--strategy", "git-tag"];

      if (!string.IsNullOrEmpty(command.Tag))
      {
        args.Add("--tag");
        args.Add(command.Tag);
      }

      int exitCode = await Shell.Builder("ganda")
        .WithArguments([.. args])
        .WithNoValidation()
        .RunAsync(ct);

      if (exitCode != 0)
      {
        Environment.ExitCode = exitCode;
      }

      return Value;
    }
  }
}
