#region Purpose
// ApplicationState action that flips the navigation column between the full width and the icon rail.
#endregion

#region Design
// The button under the user card dispatches ToggleMenu. With no stored choice the effective width
// follows the breakpoint: full at 1280px and wider, rail from 880px through 1279px. The action
// stores the opposite (twe.shellNav) and sets data-twe-nav through ShellNavScript, which is the
// same script that paints the choice before the body. IsMenuExpanded is that explicit choice
// (true is full). A runtime without the page functions still flips the in-memory choice so a
// headless send changes state. The handler does not dispatch another action.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class ToggleMenuActionSet
  {
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
        bool wide = await ShellNavScript.IsWideAsync(jsRuntime, cancellationToken);
        bool effectiveFull = stored == ShellNavScript.Full
          || (stored != ShellNavScript.Rail && wide);
        if (stored != ShellNavScript.Full && stored != ShellNavScript.Rail && ApplicationState.HasShellNavChoice)
        {
          effectiveFull = ApplicationState.IsMenuExpanded;
        }

        string next = effectiveFull ? ShellNavScript.Rail : ShellNavScript.Full;
        ApplicationState.HasShellNavChoice = true;
        ApplicationState.IsMenuExpanded = next == ShellNavScript.Full;
        await ShellNavScript.ApplyAsync(jsRuntime, next, cancellationToken);
      }
    }
  }
}
