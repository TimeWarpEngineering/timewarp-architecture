// ReSharper disable InconsistentNaming
namespace DomainInvariantsGuard_;

internal sealed class WidgetWithValidator
{
  public WidgetWithValidator(string name)
  {
    Name = name;
  }

  public string Name { get; set; }

  private sealed class Invariants : AbstractValidator<WidgetWithValidator>
  {
    public Invariants()
    {
      RuleFor(widget => widget.Name).NotEmpty();
    }
  }
}

internal sealed class WidgetWithoutValidator
{
  public string Name { get; set; } = "ok";
}

internal sealed class OtherAggregate
{
  public string Name { get; set; } = "ok";
}

// Nested validator targets a DIFFERENT type than the containing aggregate — must be treated the
// same as "no validator", not accidentally matched.
internal sealed class WidgetWithWrongTypeValidator
{
  public string Name { get; set; } = "ok";

  private sealed class Invariants : AbstractValidator<OtherAggregate>
  {
    public Invariants()
    {
      RuleFor(other => other.Name).NotEmpty();
    }
  }
}

// No parameterless constructor — Activator.CreateInstance must fail, and the guard must wrap that
// failure in MissingInvariantsValidatorException rather than let a raw reflection exception surface.
internal sealed class WidgetWithCtorlessValidator
{
  public string Name { get; set; } = "ok";

  private sealed class Invariants : AbstractValidator<WidgetWithCtorlessValidator>
  {
    public Invariants(string _)
    {
    }
  }
}

// Base declares the private validator; the subclass declares none of its own. Discovery must walk
// BaseType and find it (IValidator<in T> contravariance makes a validator for the base assignable
// to IValidator<Derived>) — this is also the proxy-compatible shape (EF dynamic proxies subclass
// non-sealed entities the same way).
internal class WidgetBaseWithValidator
{
  public WidgetBaseWithValidator(string name)
  {
    Name = name;
  }

  public string Name { get; set; }

  private sealed class Invariants : AbstractValidator<WidgetBaseWithValidator>
  {
    public Invariants()
    {
      RuleFor(widget => widget.Name).NotEmpty();
    }
  }
}

internal sealed class WidgetSubclassWithoutOwnValidator : WidgetBaseWithValidator
{
  public WidgetSubclassWithoutOwnValidator(string name) : base(name)
  {
  }
}

// Two qualifying nested validators — TWA0011/TWA0012 flag this shape at build time; the guard's job
// here is deterministic best-effort selection (prefer the private one), not ambiguity detection.
internal sealed class WidgetWithTwoValidators
{
  public WidgetWithTwoValidators(string name, string nickname)
  {
    Name = name;
    Nickname = nickname;
  }

  public string Name { get; set; }
  public string Nickname { get; set; }

  public sealed class PublicInvariants : AbstractValidator<WidgetWithTwoValidators>
  {
    public PublicInvariants()
    {
      RuleFor(widget => widget.Nickname).NotEmpty();
    }
  }

  private sealed class Invariants : AbstractValidator<WidgetWithTwoValidators>
  {
    public Invariants()
    {
      RuleFor(widget => widget.Name).NotEmpty();
    }
  }
}

public class EnsureValid_Single
{
  public void Passes_when_valid()
  {
    WidgetWithValidator widget = new("ok");
    Should.NotThrow(() => DomainInvariantsGuard.EnsureValid(widget));
  }

  public void Throws_DomainInvariantViolationException_with_failed_rule_visible()
  {
    WidgetWithValidator widget = new("");

    DomainInvariantViolationException exception =
      Should.Throw<DomainInvariantViolationException>(() => DomainInvariantsGuard.EnsureValid(widget));

    exception.AggregateType.ShouldBe(typeof(WidgetWithValidator));
    exception.FailedRules.ShouldNotBeEmpty();
  }

  public void Throws_MissingInvariantsValidatorException_when_no_nested_validator()
  {
    WidgetWithoutValidator widget = new();

    MissingInvariantsValidatorException exception =
      Should.Throw<MissingInvariantsValidatorException>(() => DomainInvariantsGuard.EnsureValid(widget));

    exception.AggregateType.ShouldBe(typeof(WidgetWithoutValidator));
  }

  public void Rejects_null_aggregate() =>
    Should.Throw<ArgumentNullException>(() => DomainInvariantsGuard.EnsureValid((object)null!));

  public void Throws_MissingInvariantsValidatorException_when_nested_validator_targets_a_different_type()
  {
    WidgetWithWrongTypeValidator widget = new();
    Should.Throw<MissingInvariantsValidatorException>(() => DomainInvariantsGuard.EnsureValid(widget));
  }

  public void Wraps_constructor_failure_as_MissingInvariantsValidatorException()
  {
    WidgetWithCtorlessValidator widget = new();

    MissingInvariantsValidatorException exception =
      Should.Throw<MissingInvariantsValidatorException>(() => DomainInvariantsGuard.EnsureValid(widget));

    exception.AggregateType.ShouldBe(typeof(WidgetWithCtorlessValidator));
    exception.InnerException.ShouldNotBeNull();
  }
}

public class EnsureValid_Many
{
  public void Validates_every_item()
  {
    WidgetWithValidator[] widgets = [new("a"), new("b")];
    Should.NotThrow(() => DomainInvariantsGuard.EnsureValid(widgets));
  }

  public void Throws_when_any_item_is_invalid()
  {
    WidgetWithValidator[] widgets = [new("a"), new("")];
    Should.Throw<DomainInvariantViolationException>(() => DomainInvariantsGuard.EnsureValid(widgets));
  }

  public void Rejects_null_collection() =>
    Should.Throw<ArgumentNullException>(() => DomainInvariantsGuard.EnsureValid((IEnumerable<object>)null!));
}

public class Discovery
{
  // WidgetWithValidator's Invariants validator is private — discovery must still find it, and
  // repeat calls (which hit the cache) must keep finding it correctly.
  public void Finds_and_caches_the_private_nested_validator_across_repeated_calls()
  {
    Should.NotThrow(() => DomainInvariantsGuard.EnsureValid(new WidgetWithValidator("first")));
    Should.NotThrow(() => DomainInvariantsGuard.EnsureValid(new WidgetWithValidator("second")));
    Should.Throw<DomainInvariantViolationException>(() => DomainInvariantsGuard.EnsureValid(new WidgetWithValidator("")));
  }

  public void Finds_a_validator_declared_on_a_base_type()
  {
    WidgetSubclassWithoutOwnValidator widget = new("ok");
    Should.NotThrow(() => DomainInvariantsGuard.EnsureValid(widget));
  }

  public void Runs_the_base_type_validator_against_the_subclass_instance()
  {
    WidgetSubclassWithoutOwnValidator widget = new("");
    Should.Throw<DomainInvariantViolationException>(() => DomainInvariantsGuard.EnsureValid(widget));
  }

  public void Prefers_the_private_candidate_when_multiple_nested_validators_qualify()
  {
    // Name is blank (violates the PRIVATE validator's rule); Nickname is present (would satisfy the
    // PUBLIC validator's rule) — the exception (and which property it names) proves the private one
    // ran, not the public one.
    WidgetWithTwoValidators widget = new(name: "", nickname: "present");

    DomainInvariantViolationException exception =
      Should.Throw<DomainInvariantViolationException>(() => DomainInvariantsGuard.EnsureValid(widget));

    exception.FailedRules.Any(rule => rule.Contains("Name", StringComparison.Ordinal)).ShouldBeTrue();
  }
}
