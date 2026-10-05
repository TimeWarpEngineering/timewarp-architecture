#region Purpose
// Client copy of one server offer (GetCredentials OfferedAction) kept in CredentialsState: catalog name, label, subject and arguments as JSON text.
#endregion

#region Design
// A plain string record, not the contract's JsonElement map: CredentialsState clones on dispatch, and
// a record of strings clones without walking JsonElement internals. ArgumentsJson is exactly what a
// contextual CommandPaletteRow carries, so the row built from an offer compares equal to the one the
// page's context source contributes (the runner's "still offered" gate is record equality).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Text.Json;

/// <param name="Name">Catalog name (<see cref="OfferedActionNames"/>).</param>
/// <param name="Label">Button / row label the server chose.</param>
/// <param name="Subject">Credential id (Guid "D") the offer applies to; null for a page-level offer.</param>
/// <param name="ArgumentsJson">JSON object of catalog arguments keyed by parameter name.</param>
public sealed record CredentialOffer(string Name, string Label, string? Subject, string ArgumentsJson)
{
  public static CredentialOffer From(OfferedAction offer) =>
    new(offer.Name, offer.Label, offer.Subject, JsonSerializer.Serialize(offer.Arguments));
}
