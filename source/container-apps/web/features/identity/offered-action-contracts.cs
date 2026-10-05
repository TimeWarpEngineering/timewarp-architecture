#region Purpose
// One client catalog action the server offers now ({ name, label, subject, arguments }), built from a typed [ActionOffer] record, and the credential-action names the server may offer.
#endregion

#region Design
// Hypermedia approach B (task 275 evaluation, adopted task 279): a read names CLIENT catalog actions
// ([CatalogAction] names such as "Credentials.RevokeCredential") with arguments keyed by the action
// constructor's parameter name (camelCase — the catalog's parameter names). The SPA resolves the name
// in IActionCatalog, binds Arguments against the entry's parameters, checks the entry is human-visible
// and permitted, and only then executes it; anything off-shape is refused with a notification. The
// catalog is the allow-list — the server can only point at actions the SPA already ships, and the real
// endpoint re-enforces the rule (the 409 backstops stay).
// Task 280 — compile-time agreement: every offerable action has ONE typed [ActionOffer] record in
// contracts (credential-action-offer-contracts.cs) naming its catalog constant here; the server builds
// offers only from those records (Create / ForCredential / ForPage are generic over them), and the
// client action sets [CatalogAction(Name = OfferedActionNames.X)]. The SPA build checks both sides:
// TWA0029 (no action carries that Name) and TWA0030 (record properties vs constructor parameters by
// camelCase name and type; UserInput covers what the user supplies). Wire shape is unchanged: the
// record serializes with the contract-seam options into the Arguments map, so the client binder keeps
// binding by parameter name and stays fail-closed for anything a record did not produce.
// Arguments are JsonElement so an offer can bind any JSON-shaped parameter. Subject names the
// credential an offer applies to (Guid "D"; null = page-level) — a display hint so a page can put the
// button on the right row without reading Arguments.
// OfferedActionNames is the server's whole vocabulary: one constant per offer record.
// Lives in Identity because Identity is the only slice that offers actions today; the day a second
// slice offers actions, OfferedAction moves to a shared contracts tier (TWA0009 keeps slices apart)
// and each slice keeps its own <Slice>OfferedActionNames + records. ActionOfferAttribute and the
// analyzers are already slice-agnostic.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Reflection;
using System.Text.Json;
using TimeWarp.Foundation.Types;

/// <summary>One catalog action the server offers now, with the arguments it binds.</summary>
public sealed class OfferedAction
{
  /// <summary>Catalog name (<see cref="OfferedActionNames"/>).</summary>
  public string Name { get; }
  public string Label { get; }
  /// <summary>Credential id (Guid "D") the offer applies to; null for a page-level offer.</summary>
  public string? Subject { get; }
  /// <summary>Arguments keyed by catalog parameter name; unbound required parameters come from the user.</summary>
  public IReadOnlyDictionary<string, JsonElement> Arguments { get; }

  public OfferedAction(string name, string label, string? subject, IReadOnlyDictionary<string, JsonElement> arguments)
  {
    Name = Guard.Against.NullOrWhiteSpace(name);
    Label = Guard.Against.NullOrWhiteSpace(label);
    Subject = subject;
    Arguments = Guard.Against.Null(arguments);
  }

  /// <summary>An offer of the action <paramref name="offer"/>'s [ActionOffer] names, with the record's properties as arguments.</summary>
  public static OfferedAction Create<TOffer>(TOffer offer, string label, string? subject) where TOffer : notnull
  {
    Dictionary<string, JsonElement> arguments = [];
    foreach (JsonProperty property in JsonSerializer.SerializeToElement(offer, ContractSerializationDefaults.Options).EnumerateObject())
    {
      arguments[property.Name] = property.Value.Clone();
    }

    return new OfferedAction(OfferName(typeof(TOffer)), label, subject, arguments);
  }

  /// <summary>An offer bound to one credential (Subject = its id): <c>{ "credentialId": "…" }</c> plus any other record properties.</summary>
  public static OfferedAction ForCredential<TOffer>(TOffer offer, string label) where TOffer : ICredentialActionOffer =>
    Create(offer, label, offer.CredentialId.ToString("D"));

  /// <summary>A page-level offer (no Subject).</summary>
  public static OfferedAction ForPage<TOffer>(TOffer offer, string label) where TOffer : notnull =>
    Create(offer, label, subject: null);

  private static string OfferName(Type offerType) =>
    offerType.GetCustomAttribute<ActionOfferAttribute>()?.CatalogName
    ?? throw new InvalidOperationException($"{offerType.Name} is not an [ActionOffer] record.");
}

/// <summary>Every catalog name the server may offer for credentials (one [ActionOffer] record each).</summary>
public static class OfferedActionNames
{
  public const string RevokeCredential = "Credentials.RevokeCredential";
  public const string RenameCredential = "Credentials.RenameCredential";
  public const string LinkMicrosoft365 = "Credentials.LinkMicrosoft365";

  public static IReadOnlyList<string> All { get; } = [RevokeCredential, RenameCredential, LinkMicrosoft365];
}
