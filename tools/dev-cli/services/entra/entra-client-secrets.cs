#region Purpose
// Client-secret mint decisions and message formatting for `dev entra setup` and `status`.
#endregion

#region Design
// Mint on --new-secret, a successful list with no ClientSecret, or a stored ClientId/TenantId
// that does not match the target (GUID-equal, case-insensitive). A failed list aborts
// (never fail-open mint — that would --append an Azure password that may never be stored).
// EntraSetup.MaskedSecret (EntraSetup) is the only display form of a secret.
#endregion

namespace DevCli.Services;

internal static class EntraClientSecrets
{
  internal static bool TryDecideMintClientSecret(
    bool newSecret,
    bool listSucceeded,
    bool hasExistingClientSecret,
    string? existingClientId,
    string? existingTenantId,
    string? targetClientId,
    string? targetTenantId,
    out bool mint,
    out bool appRegistrationChanged)
  {
    appRegistrationChanged = false;
    if (newSecret)
    {
      mint = true;
      return true;
    }

    if (!listSucceeded)
    {
      mint = false;
      return false;
    }

    if (!hasExistingClientSecret)
    {
      mint = true;
      return true;
    }

    bool sameApp = ObjectIdsEqual(existingClientId, targetClientId)
      && ObjectIdsEqual(existingTenantId, targetTenantId);
    if (!sameApp)
    {
      mint = true;
      appRegistrationChanged = true;
      return true;
    }

    mint = false;
    return true;
  }

  internal static bool ObjectIdsEqual(string? left, string? right)
  {
    string leftTrimmed = left?.Trim() ?? "";
    string rightTrimmed = right?.Trim() ?? "";
    if (leftTrimmed.Length == 0 && rightTrimmed.Length == 0)
    {
      return true;
    }

    if (leftTrimmed.Length == 0 || rightTrimmed.Length == 0)
    {
      return false;
    }

    return EntraTenants.TenantIdsEqual(leftTrimmed, rightTrimmed);
  }

  internal static string FormatAppRegistrationChangedMessage(string? existingClientId, string? existingTenantLabel)
  {
    string oldClient = string.IsNullOrWhiteSpace(existingClientId) ? "(unknown)" : existingClientId.Trim();
    string oldTenant = string.IsNullOrWhiteSpace(existingTenantLabel) ? "(unknown)" : existingTenantLabel.Trim();
    return $"App registration changed (was `{oldClient}` in `{oldTenant}`); minting a new client secret.";
  }

  internal static string FormatMintDryRunDecision(bool mint, bool newSecret, bool appRegistrationChanged)
  {
    if (!mint)
    {
      return "dry-run: keeping the existing client secret.";
    }

    if (newSecret)
    {
      return "dry-run: minting a client secret (--new-secret).";
    }

    if (appRegistrationChanged)
    {
      return "dry-run: minting a client secret (app registration changed).";
    }

    return "dry-run: minting a client secret (none in user secrets).";
  }

  internal static string FormatClientSecretSummary(bool minted, bool appRegistrationChanged)
  {
    if (!minted)
    {
      return "(unchanged)";
    }

    if (appRegistrationChanged)
    {
      return $"{EntraSetup.MaskedSecret} (minted: app registration changed)";
    }

    return $"{EntraSetup.MaskedSecret} (minted this run)";
  }

  internal static bool StoredClientIdDiffersFromFoundApp(string? storedClientId, string? foundAppId)
  {
    if (string.IsNullOrWhiteSpace(storedClientId) || string.IsNullOrWhiteSpace(foundAppId))
    {
      return false;
    }

    return !ObjectIdsEqual(storedClientId, foundAppId);
  }

  internal static string FormatClientIdMismatchWarning(string storedClientId, string foundAppId) =>
    $"Warning: stored ClientId {storedClientId} does not match the app registration {foundAppId} found by name in this tenant. Run `dev entra setup` to re-mint.";
}
