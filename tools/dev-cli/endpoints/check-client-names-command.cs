#region Purpose
// Fails when a client name appears in packed template content.
#endregion

#region Design
// Reads documentation/developer/client-names.txt (unpacked) and scans the packed trees.
// dev build and dev lint-skills call the same scan. CI's fast job runs dev build;
// a skills-only PR runs dev lint-skills. ganda repo audit has no word-list check.
#endregion

namespace DevCli.Commands;

[NuruRoute("check-client-names", Description = "Fail when a client name appears in packed template content")]
internal sealed class CheckClientNamesCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<CheckClientNamesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public Task<Unit> Handle(CheckClientNamesCommand command, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Task.FromResult(Unit.Value);
      }

      ClientNameScan.Report(
        root,
        message => Terminal.WriteLine(message),
        message => Terminal.WriteErrorLine(message.Red()));
      return Task.FromResult(Unit.Value);
    }
  }
}
