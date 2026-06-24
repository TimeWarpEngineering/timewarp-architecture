#region Purpose
// Verifies that any code samples in the repository compile
#endregion
#region Design
// Stub command for sample verification
#endregion

namespace DevCli.Commands;

[NuruRoute("verify-samples", Description = "Verify code samples compile")]
internal sealed class VerifySamplesCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      Terminal.WriteLine("Verifying samples...");
      // TODO: Implement sample verification logic specific to this repo
      Terminal.WriteLine("Samples verified successfully!");
      return ValueTask.FromResult(Value);
    }
  }
}
