namespace TimeWarp.Architecture.Foundation.Domain.Tests;

using System.Reflection;

/// <summary>Two distinct concrete <see cref="Entity{TId}"/> subclasses sharing the same TId type,
/// used to exercise the exact-type equality rule.</summary>
internal sealed class Widget : Entity<Guid>
{
  public Widget(Guid id) : base(id) { }

  public Widget(Guid id, long version) : base(id, version) { }
}

internal sealed class Gadget : Entity<Guid>
{
  public Gadget(Guid id) : base(id) { }
}

public class Equality
{
  public void Same_type_and_id_are_equal()
  {
    Guid id = Guid.NewGuid();
    Widget a = new(id);
    Widget b = new(id);

    a.Equals(b).ShouldBeTrue();
    (a == b).ShouldBeTrue();
    (a != b).ShouldBeFalse();
    a.GetHashCode().ShouldBe(b.GetHashCode());
  }

  public void Same_instance_is_equal()
  {
    Widget widget = new(Guid.NewGuid());
    widget.Equals(widget).ShouldBeTrue();
  }

  public void Different_ids_are_not_equal()
  {
    Widget a = new(Guid.NewGuid());
    Widget b = new(Guid.NewGuid());

    a.Equals(b).ShouldBeFalse();
    (a == b).ShouldBeFalse();
    (a != b).ShouldBeTrue();
  }

  public void Same_id_different_type_is_not_equal()
  {
    Guid id = Guid.NewGuid();
    Widget widget = new(id);
    Gadget gadget = new(id);

    widget.Equals(gadget).ShouldBeFalse();
  }

  public void Null_is_not_equal()
  {
    Widget widget = new(Guid.NewGuid());
    Widget? other = Null();

    widget.Equals(other).ShouldBeFalse();
    (widget == other).ShouldBeFalse();
    (other == widget).ShouldBeFalse();
  }

  public void Two_nulls_are_equal() => (Null() == Null()).ShouldBeTrue();

  // Indirection so CA1508 cannot prove the value is always null from local flow — the point of
  // these tests is exercising the operator's actual null handling, not tautological comparisons.
  private static Widget? Null() => null;
}

public class VersionTests
{
  public void Defaults_to_zero()
  {
    Widget widget = new(Guid.NewGuid());
    widget.Version.ShouldBe(0);
  }

  public void Has_no_public_setter()
  {
    PropertyInfo? property = typeof(Widget).GetProperty(nameof(Entity<Guid>.Version));
    property.ShouldNotBeNull();
    (property!.SetMethod?.IsPublic ?? false).ShouldBeFalse();
  }

  public void Rehydration_constructor_sets_version()
  {
    Widget widget = new(Guid.NewGuid(), 7);
    widget.Version.ShouldBe(7);
  }

  public void Rehydration_constructor_accepts_zero()
  {
    Widget widget = new(Guid.NewGuid(), 0);
    widget.Version.ShouldBe(0);
  }

  public void Rehydration_constructor_rejects_negative_version()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => new Widget(Guid.NewGuid(), -1));
  }
}

public class IdTests
{
  public void Is_assigned_from_constructor()
  {
    Guid id = Guid.NewGuid();
    Widget widget = new(id);
    widget.Id.ShouldBe(id);
  }

  public void Has_no_public_setter()
  {
    PropertyInfo? property = typeof(Widget).GetProperty(nameof(Entity<Guid>.Id));
    property.ShouldNotBeNull();
    (property!.SetMethod?.IsPublic ?? false).ShouldBeFalse();
  }
}
