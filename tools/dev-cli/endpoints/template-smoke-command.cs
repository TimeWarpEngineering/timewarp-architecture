#region Purpose
// Pack, install, generate, and build the template to catch sourceName/package dual-mode regressions.
#endregion
#region Design
// Thin Nuru route: options plus a Handler that delegates to TemplateSmokeRunner (services/template-smoke/).
// The variant matrix lives in TemplateSmokeMatrix, pack/install in TemplateSmokePackaging, and shared
// asserts in the types TemplateSmokeHarness composes; each carries the rationale for the behavior it owns.
#endregion

namespace DevCli.Commands;

[NuruRoute("template-smoke", Description = "Pack/install template and smoke-build the default generated app (--matrix: each flag off too)")]
internal sealed class TemplateSmokeCommand : ICommand<Unit>
{
  [Option("matrix", "m", Description = "Smoke every SmokeMatrix variant (defaults + each flag off), not only SmokeDefault")]
  public bool Matrix { get; set; }

  [Option("app-tests", Description = "Also run each generated app's test projects (aspire, integration, Playwright; needs Docker)")]
  public bool AppTests { get; set; }

  internal sealed class Handler : ICommandHandler<TemplateSmokeCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(TemplateSmokeCommand command, CancellationToken ct)
    {
      await new TemplateSmokeRunner(Terminal).RunAsync(command.Matrix, command.AppTests, ct);
      return Unit.Value;
    }
  }
}
