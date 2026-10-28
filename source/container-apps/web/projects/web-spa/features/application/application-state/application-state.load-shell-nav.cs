#region Purpose
// Restores the shell nav width choice from this browser into ApplicationState.
#endregion

#region Design
// TimeWarpPage dispatches this on its first interactive render. The head script has already set
// data-twe-nav, so this read does not change the painted width. An absent key leaves the
// breakpoint default (HasShellNavChoice false). A runtime that cannot read storage leaves the
// default too. Not a palette command: the shell runs it.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class LoadShellNavActionSet
  {
    [CommandPaletteOptOut("Startup read of the shell nav preference; the shell runs it.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      IJSRuntime jsRuntime
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        string? stored = await ShellNavScript.ReadAsync(jsRuntime, cancellationToken);
        if (stored == ShellNavScript.Full)
        {
          ApplicationState.HasShellNavChoice = true;
          ApplicationState.IsMenuExpanded = true;
        }
        else if (stored == ShellNavScript.Rail)
        {
          ApplicationState.HasShellNavChoice = true;
          ApplicationState.IsMenuExpanded = false;
        }
      }
    }
  }
}
