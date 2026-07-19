#region Purpose
// Thrown by DomainInvariantsGuard when EnsureValid was asked to validate a type with no discoverable
// nested Invariants validator — the fail-closed backstop for "must-have-a-validator" (TWA0011
// catches the same defect at build time).
#endregion

#region Design
// The three standard exception constructors (CA1032) exist for framework/test scaffolding; the two
// aggregate-aware constructors are the ones DomainInvariantsGuard actually uses:
//   - Type-only: no qualifying nested validator was found at all.
//   - Type + inner Exception: a qualifying validator type WAS found but could not be instantiated
//     (e.g. Activator.CreateInstance threw MissingMethodException for a validator with no
//     parameterless constructor) — the guard wraps that failure here instead of letting a raw
//     reflection exception surface, so the message still points at the convention.
// The message deliberately does not assert the target "implements IAggregateRoot" — EnsureValid has
// no such check (any object without a discoverable validator produces this exception) — and points
// at the in-repo exemplar (web-domain/aggregates/profile/profile.cs + aggregates/overview.md)
// instead of timewarp-identity, whose Principal/Credential have no nested Invariants validator and
// whose source is excluded from generated apps under the default foundationPackages=true.
#endregion

namespace TimeWarp.Foundation.Application.Exceptions;

public sealed class MissingInvariantsValidatorException : Exception
{
  public MissingInvariantsValidatorException()
  {
  }

  public MissingInvariantsValidatorException(string message) : base(message)
  {
  }

  public MissingInvariantsValidatorException(string message, Exception innerException) : base(message, innerException)
  {
  }

  public MissingInvariantsValidatorException(Type aggregateType)
    : base(BuildMessage(aggregateType))
  {
    AggregateType = aggregateType;
  }

  public MissingInvariantsValidatorException(Type aggregateType, Exception innerException)
    : base(BuildConstructionFailureMessage(aggregateType, innerException), innerException)
  {
    AggregateType = aggregateType;
  }

  public Type? AggregateType { get; }

  private static string BuildMessage(Type aggregateType) =>
    $"{aggregateType.Name} was validated as an aggregate root but declares no nested Invariants " +
    $"validator (a private nested class assignable to IValidator<{aggregateType.Name}>). " +
    "See the golden aggregate pattern exemplar (web-domain/aggregates/profile/profile.cs and " +
    "aggregates/overview.md) and analyzer rule TWA0011.";

  private static string BuildConstructionFailureMessage(Type aggregateType, Exception innerException) =>
    $"{aggregateType.Name} declares a nested Invariants validator, but it could not be constructed " +
    $"({innerException.GetType().Name}: {innerException.Message}). Invariants validators must have a " +
    "parameterless constructor (any accessibility) — see the golden aggregate pattern exemplar " +
    "(web-domain/aggregates/profile/profile.cs and aggregates/overview.md).";
}
