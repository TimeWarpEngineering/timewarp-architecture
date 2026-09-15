#region Purpose
// ClearSiteSettings: drop the cached snapshot on sign-out.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

partial class SiteSettingsState
{
  internal static class ClearSiteSettingsActionSet
  {
    internal sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override Task Handle(Action action, CancellationToken cancellationToken)
      {
        SiteSettingsState.Initialize();
        return Task.CompletedTask;
      }
    }
  }
}
