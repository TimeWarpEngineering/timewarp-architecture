#region Purpose
// Contributes the server's credential offers as contextual Ctrl-K rows while Settings or Passkeys is the current page.
#endregion

#region Design
// The ICommandPaletteContextSource for credentials (task 279): answers only for SettingsPage.GetPageUrl() and
// PasskeysPage.GetPageUrl() and reads CredentialsState.Offers through CredentialOfferRows, so palette rows
// follow the latest GetCredentials snapshot and vanish when the user leaves the page — no set/clear
// actions. Each page contributes the rows it shows: Settings lists passkeys and Microsoft 365 accounts
// and owns the page-level Link Microsoft 365 offer; Passkeys lists passkeys only. Rows that need input
// (Rename) are contributed too — the page's button passes the same "still offered" gate — and the
// palette leaves them out. SettingsPage is Applications (platform) chrome, so naming its route from
// Identity is the allowed direction. Registered scoped in program.cs.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Identity;

public sealed class CredentialsContextSource : ICommandPaletteContextSource
{
  private static readonly CredentialType[] SettingsTypes = [CredentialType.Passkey, CredentialType.EntraAccount];
  private static readonly CredentialType[] PasskeysTypes = [CredentialType.Passkey];

  private readonly IStore Store;
  private readonly IActionCatalog ActionCatalog;

  public CredentialsContextSource(IStore store, IActionCatalog actionCatalog)
  {
    Store = store;
    ActionCatalog = actionCatalog;
  }

  public IReadOnlyList<CommandPaletteRow> GetRows(string currentPath)
  {
    if (string.Equals(currentPath, SettingsPage.GetPageUrl(), StringComparison.OrdinalIgnoreCase))
    {
      return CredentialOfferRows.ForPage(Store.GetState<CredentialsState>(), ActionCatalog, SettingsTypes, includePageLevel: true);
    }

    if (string.Equals(currentPath, PasskeysPage.GetPageUrl(), StringComparison.OrdinalIgnoreCase))
    {
      return CredentialOfferRows.ForPage(Store.GetState<CredentialsState>(), ActionCatalog, PasskeysTypes, includePageLevel: false);
    }

    return [];
  }
}
