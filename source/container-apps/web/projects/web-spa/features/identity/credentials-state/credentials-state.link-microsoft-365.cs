#region Purpose
// LinkMicrosoft365: full-page navigation to the BFF Entra challenge (mode=link) that attaches a Microsoft 365 account to the signed-in principal.
#endregion

#region Design
// RFC 219 D10: Entra is a named BFF scheme, not a WASM MSAL session, so linking is a forceLoad
// navigation the server answers with the challenge redirect; the callback returns to /Settings
// (literal path: Settings is another slice) where the linked account is listed.
// Cataloged for humans with the Settings page's credential permission so the Ctrl-K palette
// offers it. It is listed whenever the principal holds that permission; when it does not apply
// the challenge flow reports it (404 scheme not registered, 403 site policy off, and the link
// callback's own already-linked handling). A catalog "available now" predicate (Microsoft 365
// offered AND no active EntraAccount — CanLinkMicrosoft365) would hide it instead; the catalog has
// no such hook today.
// DisplayName is authored (task 268): the generated label split "Microsoft365" into
// "microsoft 365"; the palette shows "Credentials: Link Microsoft 365".
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using Microsoft.AspNetCore.Components;

partial class CredentialsState
{
  public static class LinkMicrosoft365ActionSet
  {
    [CatalogAction
    (
      DisplayName = "Link Microsoft 365",
      Description = "Link a Microsoft 365 account to the signed-in account so it can sign in with Microsoft 365.",
      Permissions = [PermissionIds.CredentialManageSelf],
      Visibility = ActionVisibility.Human
    )]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, NavigationManager navigationManager) : BaseHandler<Action>(store)
    {
      internal const string Mode = "link";
      internal const string ReturnPath = "/Settings";

      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        navigationManager.NavigateTo(ChallengeEntra.GetRoute(Mode, ReturnPath), forceLoad: true);
        return default;
      }
    }
  }
}
