#region Purpose
// Fails when a client name appears in a tracked file outside the excluded trees.
#endregion

#region Design
// Reads documentation/developer/client-names.txt and runs the shared git grep scan.
// dev build and dev lint-skills call the same scan. CI's fast job runs dev build;
// a skills-only PR runs dev lint-skills. ganda repo audit has no word-list check.
#endregion

namespace DevCli.Commands;

[NuruRoute("check-client-names", Description = "Fail when a tracked file contains a client name")]
internal sealed class CheckClientNamesCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<CheckClientNamesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(CheckClientNamesCommand command, CancellationToken cancellationToken)
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      await ClientNameScan.ReportAsync(
        root,
        message => Terminal.WriteLine(message),
        message => Terminal.WriteErrorLine(message.Red()),
        cancellationToken);
      return Unit.Value;
    }
  }
}
