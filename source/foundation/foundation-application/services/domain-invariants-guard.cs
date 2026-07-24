#region Purpose
// Enforcement half of the nested-Invariants pattern: validates changed aggregate roots against
// their own nested Invariants validator before a save is allowed to proceed.
#endregion

#region Design
// EF-agnostic core: this type has no EF Core dependency and no DI interface — AggregateDbContext
// (and any host that inherits it) calls the static EnsureValid overloads from SaveChanges(Async)
// after resolving Added/Modified IAggregateRoot entries (including roots reached from dirty
// children). Kept static/pure so the hook needs no constructor plumbing.
// Discovery walks the aggregate's exact runtime type, then its BaseType chain (object last), looking
// at each level's own nested types for the first non-abstract one assignable to
// IValidator<TAggregate>. Walking the chain (not just the runtime type) exists for ONE
// sanctioned shape and tolerates a second, unsanctioned one as a courtesy:
//   - Sanctioned: an EF dynamic proxy (UseLazyLoadingProxies/change-tracking proxies subclass
//     non-sealed entities, so GetType() returns the proxy type, whose own GetNestedTypes() is
//     empty). The analyzer never sees proxy types — they are generated at runtime — so this is the
//     only shape the walk exists FOR.
//   - Tolerated, not authored: a validator declared on an abstract aggregate base rather than on
//     every concrete leaf also resolves here (FluentValidation's IValidator<in T> is
//     contravariant, so a validator implementing IValidator<Base> IS assignable to
//     IValidator<Derived>) — but TWA0011 deliberately still requires every concrete leaf to
//     declare its OWN nested validator (see aggregate-invariants-analyzer.cs's Design region), so a
//     hand-authored base-declared-only validator does not build warning-free. The guard resolving it
//     anyway is a side effect of the proxy-support walk, not a second authoring shape this pattern
//     endorses — do not rely on it in new code; TWA0011 is the actual authored contract.
// Generic aggregate roots remain out of scope: reflection's GetNestedTypes() on a closed
// constructed generic type returns open nested-type definitions, so discovery does not work for
// `Aggregate<T> : IAggregateRoot` today; no aggregate in this template is generic.
// Selection when a level has more than one qualifying candidate (should be rare — TWA0011/TWA0012
// flag every non-private duplicate at build time, so this is a best-effort pick, not ambiguity
// detection): prefer a private candidate (the convention-compliant shape); otherwise take the first
// match. The chosen validator is instantiated once via its parameterless constructor (any
// accessibility) and cached by runtime aggregate type so repeat saves do not pay reflection cost.
// Fail-closed by design in three directions: no nested validator is found anywhere in the chain ->
// MissingInvariantsValidatorException (an aggregate root MUST declare its invariants); a candidate
// was found but Activator.CreateInstance could not construct it (no parameterless ctor, or an
// inaccessible one) -> the same exception, wrapping the reflection failure with a cause message
// instead of letting a raw MissingMethodException/MemberAccessException surface — a genuine bug
// *inside* a validator's constructor (wrapped by Activator as TargetInvocationException) is
// deliberately NOT caught here and propagates as-is, since that is not a "missing validator" defect;
// the validator runs and reports failures -> DomainInvariantViolationException. No path lets an
// unvalidated or invalid aggregate reach the store.
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
    IValidator validator = ValidatorCache.GetOrAdd(aggregateType, DiscoverAndInstantiateValidator)
      ?? throw new MissingInvariantsValidatorException(aggregateType);

    ValidationResult result = validator.Validate(new ValidationContext<object>(aggregate));
    if (!result.IsValid)
    {
      throw new DomainInvariantViolationException(aggregateType, result.Errors);
    }
  }

  private static IValidator? DiscoverAndInstantiateValidator(Type aggregateType)
  {
    Type validatorInterface = typeof(IValidator<>).MakeGenericType(aggregateType);

    for (Type? declaringType = aggregateType; declaringType is not null; declaringType = declaringType.BaseType)
    {
      Type? validatorType = SelectValidatorType(declaringType, validatorInterface);
      if (validatorType is not null)
      {
        return Instantiate(validatorType, aggregateType);
      }
    }

    return null;
  }

  private static Type? SelectValidatorType(Type declaringType, Type validatorInterface)
  {
    Type[] candidates = declaringType
      .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
      .Where(nested => !nested.IsAbstract && validatorInterface.IsAssignableFrom(nested))
      .ToArray();

    if (candidates.Length == 0) return null;
    if (candidates.Length == 1) return candidates[0];

    return candidates.FirstOrDefault(candidate => candidate.IsNestedPrivate) ?? candidates[0];
  }

  private static IValidator Instantiate(Type validatorType, Type aggregateType)
  {
    try
    {
      return (IValidator)Activator.CreateInstance(validatorType, nonPublic: true)!;
    }
    catch (Exception ex) when (ex is MissingMethodException or MemberAccessException)
    {
      throw new MissingInvariantsValidatorException(aggregateType, ex);
    }
  }
}
