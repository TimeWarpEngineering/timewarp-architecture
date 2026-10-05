#region Purpose
// One client catalog action the server offers now ({ name, label, subject, arguments }) and the credential-action names the server may offer.
#endregion

#region Design
// Hypermedia approach B (task 275 evaluation, adopted task 279): a read names CLIENT catalog actions
// ([CatalogAction] names such as "Credentials.RevokeCredential") with arguments keyed by the action
// constructor's parameter name (camelCase — the catalog's parameter names). The SPA resolves the name
// in IActionCatalog, binds Arguments against the entry's parameters, checks the entry is human-visible
// and permitted, and only then executes it; anything off-shape is refused with a notification. The
// catalog is the allow-list — the server can only point at actions the SPA already ships, and the real
// endpoint re-enforces the rule (the 409 backstops stay).
// Arguments are JsonElement so an offer can bind any JSON-shaped parameter; an offer may leave a
// required parameter unbound (Rename leaves the nickname to the user), which the client collects on
// the page and the palette skips. Subject names the credential an offer applies to (Guid "D"; null =
// page-level) — a display hint so a page can put the button on the right row without reading
// Arguments.
// OfferedActionNames is the server's whole vocabulary: every name it can emit is a constant here, and
// a web-spa test resolves each one in the real IActionCatalog, so a renamed action breaks a test
// instead of silently vanishing from the offer. Nothing yet ties CredentialIdArgument to the action
// constructor's parameter name at compile time; that same test pins it at run time.
// Lives in Identity because Identity is the only slice that offers actions today; the day a second
// slice offers actions, OfferedAction moves to a shared contracts tier (TWA0009 keeps slices apart).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Text.Json;

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

  /// <summary>An offer bound to one credential: <c>{ "credentialId": "…" }</c>.</summary>
  public static OfferedAction ForCredential(string name, string label, CredentialId credentialId)
  {
    string subject = credentialId.Value.ToString("D");
    return new OfferedAction
    (
      name,
      label,
      subject,
      new Dictionary<string, JsonElement>
      {
        [OfferedActionNames.CredentialIdArgument] = JsonSerializer.SerializeToElement(subject)
      }
    );
  }

  /// <summary>A page-level offer with no bound arguments.</summary>
  public static OfferedAction ForPage(string name, string label) =>
    new(name, label, subject: null, new Dictionary<string, JsonElement>());
}

/// <summary>Every catalog name the server may offer for credentials, with the argument keys it binds.</summary>
public static class OfferedActionNames
{
  public const string RevokeCredential = "Credentials.RevokeCredential";
  public const string RenameCredential = "Credentials.RenameCredential";
  public const string LinkMicrosoft365 = "Credentials.LinkMicrosoft365";

  public const string CredentialIdArgument = "credentialId";

  public static IReadOnlyList<string> All { get; } = [RevokeCredential, RenameCredential, LinkMicrosoft365];
}
