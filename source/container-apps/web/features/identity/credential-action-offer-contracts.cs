#region Purpose
// Hand-written typed offer arguments for credential actions with no contract Command (Link Microsoft 365), and the interface credential-bound offers share.
#endregion

#region Design
// Task 281: offers of contract Commands are GENERATED — [Offerable] on RevokeCredential /
// RenameCredential makes the contracts generator emit their nested Offer record and OfferName, so the
// offer cannot drift from the Command. This file keeps the escape hatch: a hand-written
// [ActionOffer] record for an action that has no contract Command to generate from. Link Microsoft
// 365 is a browser redirect to a hand-written challenge endpoint (ChallengeEntraEndpoint), so its
// record stays here and binds nothing. TWA0029 / TWA0030 check hand-written and generated records
// alike (task 280).
// ICredentialActionOffer is how OfferedAction.ForCredential reads the credential id for Subject; the
// generated records opt in through a partial declaration in their contract
// (`partial record Offer : ICredentialActionOffer;`). Guid, not CredentialId: the property type must
// equal the action parameter type.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

/// <summary>An offer record bound to one credential; <see cref="OfferedAction.ForCredential{TOffer}"/> uses its id as the Subject.</summary>
public interface ICredentialActionOffer
{
  Guid CredentialId { get; }
}

/// <summary>Offer to link a Microsoft 365 account (page-level, no arguments).</summary>
[ActionOffer(OfferedActionNames.LinkMicrosoft365)]
public sealed record LinkMicrosoft365Offer;
