#region Purpose
// Maps the hypermedia lab's server payloads to contextual CommandPaletteRows — the one shape its buttons and its Ctrl-K rows share.
#endregion

#region Design
// Both approaches end in a contextual row so one runner (CommandPaletteRunner.RunContextualAsync)
// gates buttons and palette rows alike:
//   B — Target = the offered catalog name, ArgumentsJson = the server's arguments, FollowUpTarget =
//       FetchCredentialOffers (the runner refreshes the payload after the action);
//   C — Target = FollowCommand, ArgumentsJson = { method, href } (+ an empty fields map when the
//       command has no Fields; otherwise the page's input supplies fields), no follow-up (FollowCommand
//       follows Self itself).
// RequiresInput: B when the catalog entry has a required parameter the offer left unbound
// (Rename's nickname); C when the command declares Fields. Such rows keep their button (with an
// input) but are not palette rows. An offer naming an unknown catalog entry still gets a row; running
// it fails closed in the runner, which is the behaviour the evaluation wants visible.
// Names carry the approach and the credential ("Lab B: Revoke · Work laptop (3f9a1c2e)") so the
// palette rows are distinguishable and searchable.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

using static TimeWarp.Architecture.Features.HypermediaLab.GetCredentialCommands;
using static TimeWarp.Architecture.Features.HypermediaLab.GetCredentialOffers;

public static class HypermediaLabRows
{
  public const string FollowCommandMethodArgument = "method";
  public const string FollowCommandHrefArgument = "href";
  public const string FollowCommandFieldsArgument = "fields";

  /// <summary>Every row the lab's current payloads offer (palette-eligible or not).</summary>
  public static IReadOnlyList<CommandPaletteRow> All(HypermediaLabState state, IActionCatalog actionCatalog)
  {
    List<CommandPaletteRow> rows = [];
    if (state.Offers is { } offers)
    {
      rows.AddRange(offers.Offers.Select(offer => OfferRow(offers, offer, actionCatalog)));
    }

    if (state.Commands is { } commands)
    {
      rows.AddRange(commands.Commands.Select(command => CommandRow(commands, command)));
    }

    return rows;
  }

  /// <summary>Approach B: run the offered catalog action with the server's arguments, then refresh the offers.</summary>
  public static CommandPaletteRow OfferRow(GetCredentialOffers.Response payload, OfferedAction offer, IActionCatalog actionCatalog) =>
    new
    (
      $"Lab B: {offer.Label}{SubjectSuffix(payload.Credentials, offer.Subject)}",
      $"Server-offered catalog action {offer.Name}.",
      CommandPaletteRowKind.Contextual,
      offer.Name,
      JsonSerializer.Serialize(offer.Arguments),
      HypermediaLabState.FetchCredentialOffersActionSet.CatalogName,
      UnboundParameters(actionCatalog.Find(offer.Name), offer).Count > 0
    );

  /// <summary>Approach C: follow the server's link command.</summary>
  public static CommandPaletteRow CommandRow(GetCredentialCommands.Response payload, LinkCommand command) =>
    new
    (
      $"Lab C: {command.Label}{SubjectSuffix(payload.Credentials, command.Subject)}",
      $"Server link: {command.Method} {command.Href}",
      CommandPaletteRowKind.Contextual,
      HypermediaLabState.FollowCommandActionSet.CatalogName,
      FollowCommandArguments(command),
      FollowUpTarget: null,
      RequiresInput: command.Fields.Count > 0
    );

  /// <summary>{ method, href } plus an empty fields map when the command needs no input (the page supplies it otherwise).</summary>
  public static string FollowCommandArguments(LinkCommand command)
  {
    Dictionary<string, object> arguments = new(StringComparer.Ordinal)
    {
      [FollowCommandMethodArgument] = command.Method,
      [FollowCommandHrefArgument] = command.Href
    };
    if (command.Fields.Count == 0)
    {
      arguments[FollowCommandFieldsArgument] = new Dictionary<string, string>();
    }

    return JsonSerializer.Serialize(arguments);
  }

  /// <summary>Required catalog parameters the offer leaves for the user; empty for an unknown entry.</summary>
  public static IReadOnlyList<string> UnboundParameters(ActionCatalogEntry? entry, OfferedAction offer) =>
    entry is null
      ? []
      : [.. entry.Parameters.Where(parameter => parameter.IsRequired && !offer.Arguments.ContainsKey(parameter.Name)).Select(static parameter => parameter.Name)];

  /// <summary>" · Work laptop (3f9a1c2e)" for a credential subject; empty for a page-level one.</summary>
  public static string SubjectSuffix(IReadOnlyList<GetCredentials.CredentialSummary> credentials, string? subject)
  {
    if (subject is null)
    {
      return "";
    }

    GetCredentials.CredentialSummary? credential =
      credentials.FirstOrDefault(candidate => candidate.Id.Value.ToString("D") == subject);
    return credential is null
      ? $" · {subject}"
      : $" · {credential.Nickname ?? credential.Label ?? credential.Type.ToString()} ({credential.Fingerprint})";
  }
}
