#region Purpose
// Enforcement half of the nested-Invariants pattern: validates changed aggregate roots against
// their own nested Invariants validator before a save is allowed to proceed.
#endregion

#region Design
// EF-agnostic core: this type has no EF Core dependency and no DI interface — hosts call the static
// EnsureValid overloads directly from their DbContext.SaveChanges(Async) override (e.g.
// PostgresDbContext), after filtering entries down to Added/Modified IAggregateRoot instances. Kept
// static/pure so the hook needs no constructor plumbing.
// Discovery: the first nested type (public or non-public) on the aggregate's exact runtime type that
// is assignable to IValidator&lt;TAggregate&gt; is treated as its Invariants validator, instantiated
// once via the validator's parameterless constructor, and cached by aggregate type so repeat saves
// do not pay reflection cost.
// Fail-closed by design in both directions: no nested validator is found -&gt;
// MissingInvariantsValidatorException (an aggregate root MUST declare its invariants); the validator
// runs and reports failures -&gt; DomainInvariantViolationException. Neither path lets an unvalidated
// or invalid aggregate reach the store.
// TWA0011/TWA0012 duplicate this check at build time (analyzer = build-time upgrade, this guard =
// runtime backstop for any path the analyzer cannot see, e.g. dynamically loaded assemblies).
#endregion

namespace TimeWarp.Foundation.Application.Services;

using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using TimeWarp.Foundation.Application.Exceptions;

public static class DomainInvariantsGuard
{
  private static readonly ConcurrentDictionary<Type, IValidator?> ValidatorCache = new();

  public static void EnsureValid(IEnumerable<object> aggregates)
  {
    ArgumentNullException.ThrowIfNull(aggregates);

    foreach (object aggregate in aggregates)
    {
      EnsureValid(aggregate);
    }
  }

  public static void EnsureValid(object aggregate)
  {
    ArgumentNullException.ThrowIfNull(aggregate);

    Type aggregateType = aggregate.GetType();
    IValidator validator = ValidatorCache.GetOrAdd(aggregateType, DiscoverValidator)
      ?? throw new MissingInvariantsValidatorException(aggregateType);

    ValidationResult result = validator.Validate(new ValidationContext<object>(aggregate));
    if (!result.IsValid)
    {
      throw new DomainInvariantViolationException(aggregateType, result.Errors);
    }
  }

  private static IValidator? DiscoverValidator(Type aggregateType)
  {
    Type validatorInterface = typeof(IValidator<>).MakeGenericType(aggregateType);

    Type? validatorType = aggregateType
      .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
      .FirstOrDefault(nested => !nested.IsAbstract && validatorInterface.IsAssignableFrom(nested));

    return validatorType is null ? null : (IValidator)Activator.CreateInstance(validatorType, nonPublic: true)!;
  }
}
