#region Purpose
// FetchSession: read whether the browser has a signed-in identity session (GetCurrentSession).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Services;

partial class SignInState
{
  public static class FetchSessionActionSet
  {
    [CommandPaletteOptOut("Page/startup data load; the owning page or shell runs it, a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, PasskeyCeremonyClient ceremony) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        SignInState.IsAuthenticated = await ceremony.GetIsAuthenticatedAsync(cancellationToken);
      }
    }
  }
}
