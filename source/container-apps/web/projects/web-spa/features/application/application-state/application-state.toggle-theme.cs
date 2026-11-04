#region Purpose
// ApplicationState action that switches Fluent between the light and the dark theme.
#endregion

#region Design
// The top-bar theme button dispatches ToggleTheme. Fluent owns the mode: SwitchThemeAsync flips
// the effective mode (the stored mode, else the OS preference), stores the new mode under
// fluentui-blazor:theme-settings, and sets or removes body[data-theme="dark"]. tokens.css keys
// the dark --twe-* values on that attribute, so the shell and Fluent change together. On reload
// BrandThemeScript merges the seed onto the stored settings and keeps the mode, so the choice
// survives. This store keeps no copy of the mode. The handler does not dispatch another action.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class ToggleThemeActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      IThemeService themeService
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        _ = await themeService.SwitchThemeAsync();
      }
    }
  }
}
