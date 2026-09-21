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
// no such check (any object without a discoverable validator produces this exception). Messages are
// self-directed: they state the fix (declare a private nested Invariants validator, rule TWA0011)
// rather than pointing at a file path in this particular consumer's disk layout — a published
// foundation package cannot assume every consumer's template layout matches this monorepo's. The
// in-package XML `<example>` on IAggregateRoot (foundation-domain) carries the worked example.
#endregion

namespace TimeWarp.Foundation.Application.Exceptions;

/// <summary>
/// Raised when <see cref="TimeWarp.Foundation.Application.Services.DomainInvariantsGuard"/> cannot find or construct a nested <c>Invariants</c> validator.
/// </summary>
public sealed class MissingInvariantsValidatorException : Exception
{
  /// <summary>
  /// Creates an empty missing-validator exception for framework scaffolding.
  /// </summary>
  public MissingInvariantsValidatorException()
  {
  }

  /// <summary>
  /// Creates a missing-validator exception with a custom message.
  /// </summary>
  public MissingInvariantsValidatorException(string message) : base(message)
  {
  }

  /// <summary>
  /// Creates a missing-validator exception with a custom message and inner exception.
  /// </summary>
  public MissingInvariantsValidatorException(string message, Exception innerException) : base(message, innerException)
  {
  }

  /// <summary>
  /// Creates a missing-validator exception for an aggregate with no discoverable nested validator.
  /// </summary>
  public MissingInvariantsValidatorException(Type aggregateType)
    : base(BuildMessage(aggregateType))
  {
    AggregateType = aggregateType;
  }

  /// <summary>
  /// Creates a missing-validator exception when a nested validator exists but cannot be constructed.
  /// </summary>
  public MissingInvariantsValidatorException(Type aggregateType, Exception innerException)
    : base(BuildConstructionFailureMessage(aggregateType, innerException), innerException)
  {
    AggregateType = aggregateType;
  }

  /// <summary>
  /// Runtime type that lacked a usable nested <c>Invariants</c> validator, when known.
  /// </summary>
  public Type? AggregateType { get; }

  private static string BuildMessage(Type aggregateType) =>
    $"{aggregateType.Name} was validated as an aggregate root but declares no nested Invariants " +
    $"validator. Declare a private sealed class Invariants : AbstractValidator<{aggregateType.Name}> " +
    $"nested in {aggregateType.Name} with the aggregate's full rule set (rule TWA0011). See the " +
    "IAggregateRoot XML docs for a worked example.";

  private static string BuildConstructionFailureMessage(Type aggregateType, Exception innerException) =>
    $"{aggregateType.Name} declares a nested Invariants validator, but it could not be constructed " +
    $"({innerException.GetType().Name}: {innerException.Message}). Invariants validators must have a " +
    "parameterless constructor (any accessibility). See the IAggregateRoot XML docs for a worked example.";
}
