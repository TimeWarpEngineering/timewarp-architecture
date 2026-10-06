#region Purpose
// Marks an endpoint contract whose Command the server may offer as a client catalog action; the contracts generator emits its Offer record and OfferName.
#endregion

#region Design
// Task 281: an offer is the contract's Command minus server-filled fields, split into what the server
// binds and what the user types. Hand-written [ActionOffer] records copied that shape and could drift
// from the contract, so the contract carries this flag and the contracts generator
// (TimeWarp.Foundation.Contracts) emits, onto the contract's partial class, a nested
// `[ActionOffer(OfferName, UserInput = …)] sealed partial record Offer(…)` plus `const string OfferName`.
// The existing TWA0029/TWA0030 check the generated record like a hand-written one, and TWA0031 links
// the contract to the client action whose handler requests its Command.
// UserInput takes Command PROPERTY names (nameof(Command.Nickname)) so a rename is a compile error
// here; the generator turns them into the camelCase parameter names [ActionOffer] carries.
// Hand-written [ActionOffer] records stay the escape hatch for offers with no contract Command (a
// browser redirect to a hand-written endpoint, a client-only action).
// Matched by simple name, like every convention-analyzer attribute.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that the server may offer this contract's <c>Command</c> as a client catalog action. The
/// contracts generator emits a nested <c>Offer</c> record (route parameters plus Command properties,
/// minus <see cref="UserInput"/> and the auth-filled <c>UserId</c>) and an <c>OfferName</c> constant;
/// the client action whose handler requests the Command must set <c>[CatalogAction(Name = OfferName)]</c>
/// (TWA0031).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OfferableAttribute : Attribute
{
  /// <summary>Command property names the user supplies (use <c>nameof(Command.X)</c>); the server binds every other non-auth property.</summary>
  public string[] UserInput { get; set; } = [];
}
