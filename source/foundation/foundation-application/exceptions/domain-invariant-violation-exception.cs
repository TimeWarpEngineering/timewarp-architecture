#region Purpose
// Thrown by DomainInvariantsGuard when an aggregate root's nested Invariants validator rejects the
// current state — invalid state can never be persisted regardless of which code path mutated it.
#endregion

#region Design
// Carries the aggregate's runtime type name and the failed rule messages so a handler or logger can
// report *what* was invalid without re-running the validator. The three standard exception
// constructors (CA1032) exist for framework/test scaffolding; the aggregate-aware constructor is the
// one DomainInvariantsGuard actually uses.
#endregion

namespace TimeWarp.Foundation.Application.Exceptions;

using FluentValidation.Results;

/// <summary>
/// Raised when an aggregate root fails its nested <c>Invariants</c> validator before persistence.
/// </summary>
public sealed class DomainInvariantViolationException : Exception
{
  /// <summary>
  /// Creates an empty violation exception for framework scaffolding.
  /// </summary>
  public DomainInvariantViolationException()
  {
    FailedRules = [];
  }

  /// <summary>
  /// Creates a violation exception with a custom message.
  /// </summary>
  public DomainInvariantViolationException(string message) : base(message)
  {
    FailedRules = [];
  }

  /// <summary>
  /// Creates a violation exception with a custom message and inner exception.
  /// </summary>
  public DomainInvariantViolationException(string message, Exception innerException) : base(message, innerException)
  {
    FailedRules = [];
  }

  /// <summary>
  /// Creates a violation exception from the aggregate type and FluentValidation failures.
  /// </summary>
  public DomainInvariantViolationException(Type aggregateType, IReadOnlyList<ValidationFailure> failures)
    : base(BuildMessage(aggregateType, failures))
  {
    AggregateType = aggregateType;
    FailedRules = failures.Select(failure => failure.ErrorMessage).ToArray();
  }

  /// <summary>
  /// Runtime type of the aggregate that failed validation, when known.
  /// </summary>
  public Type? AggregateType { get; }

  /// <summary>
  /// Failed invariant rule messages from the nested validator.
  /// </summary>
  public IReadOnlyList<string> FailedRules { get; }

  private static string BuildMessage(Type aggregateType, IReadOnlyList<ValidationFailure> failures) =>
    $"{aggregateType.Name} violates its domain invariants: {string.Join("; ", failures.Select(failure => failure.ErrorMessage))}";
}
