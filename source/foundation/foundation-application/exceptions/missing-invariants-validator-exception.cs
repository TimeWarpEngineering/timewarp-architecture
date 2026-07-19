#region Purpose
// Thrown by DomainInvariantsGuard when an IAggregateRoot has no nested Invariants validator — the
// fail-closed backstop for "must-have-a-validator" (TWA0011 catches the same defect at build time).
#endregion

#region Design
// The three standard exception constructors (CA1032) exist for framework/test scaffolding; the
// aggregate-aware constructor is the one DomainInvariantsGuard actually uses, and its message
// points at both the convention (nested private class assignable to IValidator&lt;T&gt;) and the
// build-time analyzer so the fix is discoverable from the exception alone.
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

  public Type? AggregateType { get; }

  private static string BuildMessage(Type aggregateType) =>
    $"{aggregateType.Name} implements IAggregateRoot but declares no nested Invariants validator " +
    "(a private nested class assignable to IValidator<" + aggregateType.Name + ">). " +
    "See the golden aggregate pattern (source/libraries/timewarp-identity) and analyzer rule TWA0011.";
}
