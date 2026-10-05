#region Purpose
// Maps CredentialsState's server offers to contextual CommandPaletteRows — the one shape the Settings / Passkeys buttons and their Ctrl-K rows share.
#endregion

#region Design
// Hypermedia approach B (task 279): a button and its palette row are the same contextual row, run by
// CommandPaletteRunner.RunContextualAsync, so both pass one fail-closed gate (still offered by the
// current page → known catalog entry → human-visible and permitted → arguments bind). Target is the
// offered catalog name, ArgumentsJson the server's arguments, FollowUpTarget FetchCredentials — the
// runner refreshes the list and its offers afterwards (handlers never dispatch, TWS0002).
// RequiresInput is set when the catalog entry has a required parameter the offer left unbound
// (Rename's nickname): the page collects it, the palette (no argument UI) leaves the row out. An
// offer naming an unknown entry still maps to a row; running it fails closed in the runner.
// Names read "Credentials: Revoke · Work laptop (3f9a1c2e)" — the palette's "Owner: Action" label
// plus the row's title and fingerprint so rows for different credentials are distinguishable; a
// page-level offer is just "Credentials: <label>".
// ForPage filters by the credential types a page lists, so a page only contributes rows for the
// rows it shows (Passkeys never offers to unlink a Microsoft 365 account it does not display).
// RunAsync is the pages' one entry point: it finds the current offer and runs its row; with no offer
// it still goes through the runner with a bare row, which the runner refuses ("not offered here now")
// — the Settings / Passkeys list buttons and their Ctrl-K rows have no second path that could run
// an action the server did not offer. (AddPasskeyPrompt's "Name this passkey" Save is the one
// RenameCredential dispatch outside this runner; its Design region records why.)
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Text.Json;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Identity;
using static GetCredentials;

public static class CredentialOfferRows
{
  private const string Owner = "Credentials";

  /// <summary>Runs the current offer <paramref name="name"/> for <paramref name="credentialId"/> (null = page-level) through the catalog; refused when not offered.</summary>
  public static Task RunAsync
  (
    string name,
    Guid? credentialId,
    IReadOnlyDictionary<string, JsonElement>? input,
    IStore store,
    IActionCatalog actionCatalog,
    CommandPaletteContext context,
    CancellationToken cancellationToken
  )
  {
    CredentialsState state = store.GetState<CredentialsState>();
    CredentialOffer? offer = state.FindOffer(name, credentialId);
    CommandPaletteRow row = offer is null
      ? new CommandPaletteRow($"{Owner}: {name}", "", CommandPaletteRowKind.Contextual, name)
      : Row(offer, state.Credentials ?? [], actionCatalog);
    return CommandPaletteRunner.RunContextualAsync(row, input, store, actionCatalog, context, cancellationToken);
  }

  /// <summary>Input for RenameCredential's unbound nickname.</summary>
  public static IReadOnlyDictionary<string, JsonElement> NicknameInput(string nickname) =>
    new Dictionary<string, JsonElement> { [RenameCredentialOffer.NicknameInput] = JsonSerializer.SerializeToElement(nickname) };

  /// <summary>Rows for every offer whose credential has one of <paramref name="types"/>, plus page-level offers when <paramref name="includePageLevel"/>.</summary>
  public static IReadOnlyList<CommandPaletteRow> ForPage
  (
    CredentialsState state,
    IActionCatalog actionCatalog,
    IReadOnlyCollection<CredentialType> types,
    bool includePageLevel
  )
  {
    IReadOnlyList<CredentialSummary> credentials = state.Credentials ?? [];
    List<CommandPaletteRow> rows = [];
    foreach (CredentialOffer offer in state.Offers)
    {
      if (offer.Subject is null)
      {
        if (includePageLevel)
        {
          rows.Add(Row(offer, credentials, actionCatalog));
        }

        continue;
      }

      CredentialSummary? credential = Subject(credentials, offer.Subject);
      if (credential is not null && types.Contains(credential.Type))
      {
        rows.Add(Row(offer, credentials, actionCatalog));
      }
    }

    return rows;
  }

  /// <summary>The contextual row that runs <paramref name="offer"/> and then refreshes the credentials.</summary>
  public static CommandPaletteRow Row(CredentialOffer offer, IReadOnlyList<CredentialSummary> credentials, IActionCatalog actionCatalog)
  {
    ActionCatalogEntry? entry = actionCatalog.Find(offer.Name);
    return new CommandPaletteRow
    (
      $"{Owner}: {offer.Label}{SubjectSuffix(credentials, offer.Subject)}",
      entry?.Description ?? $"Server-offered action {offer.Name}.",
      CommandPaletteRowKind.Contextual,
      offer.Name,
      offer.ArgumentsJson,
      CredentialsState.FetchCredentialsActionSet.CatalogName,
      UnboundParameters(entry, offer).Count > 0
    );
  }

  /// <summary>Required catalog parameters the offer leaves for the user; empty for an unknown entry or unreadable arguments.</summary>
  public static IReadOnlyList<string> UnboundParameters(ActionCatalogEntry? entry, CredentialOffer offer)
  {
    if (entry is null || !ContextualActionArguments.Parse(offer.ArgumentsJson).TryPickT0(out Dictionary<string, JsonElement>? arguments, out _))
    {
      return [];
    }

    return [.. entry.Parameters.Where(parameter => parameter.IsRequired && !arguments.ContainsKey(parameter.Name)).Select(static parameter => parameter.Name)];
  }

  /// <summary>" · Work laptop (3f9a1c2e)" for a credential subject; empty for a page-level one.</summary>
  public static string SubjectSuffix(IReadOnlyList<CredentialSummary> credentials, string? subject)
  {
    if (subject is null)
    {
      return "";
    }

    CredentialSummary? credential = Subject(credentials, subject);
    return credential is null
      ? $" · {subject}"
      : $" · {CredentialRowPresenter.Title(credential, credential.Type.ToString())} ({credential.Fingerprint})";
  }

  private static CredentialSummary? Subject(IReadOnlyList<CredentialSummary> credentials, string subject) =>
    credentials.FirstOrDefault(candidate => candidate.Id.Value.ToString("D") == subject);
}
