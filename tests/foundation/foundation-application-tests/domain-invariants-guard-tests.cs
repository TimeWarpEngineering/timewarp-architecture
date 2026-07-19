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
}
