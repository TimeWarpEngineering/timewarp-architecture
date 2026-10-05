#region Purpose
// Typed arguments of every credential action the server may offer — one [ActionOffer] record per offerable catalog action.
#endregion

#region Design
// Task 280: a record's public properties ARE the offer's arguments (camelCase on the wire, the target
// action's constructor parameter names), so the server cannot spell an argument key by hand and the
// SPA build checks each record against its [CatalogAction(Name = …)] action (TWA0029 / TWA0030).
// Revoke and Rename bind the credential (ICredentialActionOffer, so OfferedAction.ForCredential sets
// Subject from the same id); Rename leaves the nickname to the user (UserInput — the page collects it,
// the palette skips the row). Link Microsoft 365 is page-level and binds nothing. Guid, not
// CredentialId: the property type must equal the action parameter type.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

/// <summary>An offer record bound to one credential; <see cref="OfferedAction.ForCredential{TOffer}"/> uses its id as the Subject.</summary>
public interface ICredentialActionOffer
{
  Guid CredentialId { get; }
}

/// <summary>Offer to revoke one credential.</summary>
[ActionOffer(OfferedActionNames.RevokeCredential)]
public sealed record RevokeCredentialOffer(Guid CredentialId) : ICredentialActionOffer;

/// <summary>Offer to rename one credential; the user supplies the nickname.</summary>
[ActionOffer(OfferedActionNames.RenameCredential, UserInput = [NicknameInput])]
public sealed record RenameCredentialOffer(Guid CredentialId) : ICredentialActionOffer
{
  /// <summary>RenameCredential's parameter the user supplies (the offer binds credentialId only).</summary>
  public const string NicknameInput = "nickname";
}

/// <summary>Offer to link a Microsoft 365 account (page-level, no arguments).</summary>
[ActionOffer(OfferedActionNames.LinkMicrosoft365)]
public sealed record LinkMicrosoft365Offer;
