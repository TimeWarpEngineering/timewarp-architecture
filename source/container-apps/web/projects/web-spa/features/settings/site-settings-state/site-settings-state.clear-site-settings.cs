#region Purpose
// ClearSiteSettings: drop the cached snapshot on sign-out.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

partial class SiteSettingsState
{
  public static class ClearSiteSettingsActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SiteSettingsState.Initialize();
        return default;
      }
    }
  }
}
