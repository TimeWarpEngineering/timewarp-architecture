#region Purpose
// RFC 219 D8: detect Entra-without-passkey from the GetCredentials Type list for the add-passkey soft prompt.
#endregion

#region Design
// Visibility is a Type-list predicate, not a TrustTier and not quarantine. An active EntraAccount
// without an active Passkey → show. Dismissed (session / later) → hide. Null snapshot (not
// fetched) → hide so the banner never flashes before GetCredentials returns. AgentKey does not
// count as a passkey. Callers render or hide UI only — this type never authorizes or redirects.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using TimeWarp.Identity;
using static GetCredentials;

public static class PasskeySoftPrompt
{
  public const string LaterStorageKey = "twe-passkey-soft-prompt-later";

  public static bool ShouldShow(IReadOnlyList<CredentialSummary>? credentials, bool dismissed)
  {
    if (dismissed || credentials is null)
    {
      return false;
    }

    bool hasActiveEntra = false;
    bool hasActivePasskey = false;
    foreach (CredentialSummary credential in credentials)
    {
      if (!credential.IsActive)
      {
        continue;
      }

      if (credential.Type == CredentialType.EntraAccount)
      {
        hasActiveEntra = true;
      }
      else if (credential.Type == CredentialType.Passkey)
      {
        hasActivePasskey = true;
      }
    }

    return hasActiveEntra && !hasActivePasskey;
  }
}
