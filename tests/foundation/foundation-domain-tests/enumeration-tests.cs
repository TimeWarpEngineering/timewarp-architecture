namespace TimeWarp.Architecture.Foundation.Domain.Tests;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A concrete <see cref="Enumeration"/> used to exercise the base class.</summary>
internal sealed class Color : Enumeration
{
  public static readonly Color Red = new(1, "Red", ["R", "FF0000"]);
  public static readonly Color Green = new(2, "Green", ["G"]);
  public static readonly Color Blue = new(3, "Blue", null);

  private Color(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

public class GetAll
{
  public void Returns_all_static_fields() =>
    Enumeration.GetAll<Color>().ToList().Count.ShouldBe(3);
}

public class FromValue
{
  public void Returns_match_for_valid_value() =>
    Enumeration.FromValue<Color>(1).ShouldBe(Color.Red);

  public void Throws_InvalidOperationException_for_invalid_value() =>
    Should.Throw<InvalidOperationException>(() => Enumeration.FromValue<Color>(99));
}

public class FromName
{
  public void Returns_match_for_valid_name() =>
    Enumeration.FromName<Color>("Green").ShouldBe(Color.Green);

  public void Throws_InvalidOperationException_for_invalid_name() =>
    Should.Throw<InvalidOperationException>(() => Enumeration.FromName<Color>("Magenta"));
}

public class FromAlternateCode
{
  public void Returns_match_for_valid_code() =>
    Enumeration.FromAlternateCode<Color>("FF0000").ShouldBe(Color.Red);

  public void Throws_InvalidOperationException_for_invalid_code() =>
    Should.Throw<InvalidOperationException>(() => Enumeration.FromAlternateCode<Color>("ZZ"));
}

public class FromString
{
  public void Returns_match_by_name() =>
    Enumeration.FromString<Color>("Blue").ShouldBe(Color.Blue);

  public void Returns_match_by_alternate_code() =>
    Enumeration.FromString<Color>("G").ShouldBe(Color.Green);

  public void Throws_InvalidOperationException_for_invalid_input() =>
    Should.Throw<InvalidOperationException>(() => Enumeration.FromString<Color>("nope"));
}

public class CompareTo
{
  public void Orders_by_value()
  {
    Color.Red.CompareTo(Color.Blue).ShouldBeLessThan(0);
    Color.Blue.CompareTo(Color.Red).ShouldBeGreaterThan(0);
    Color.Red.CompareTo(Color.Red).ShouldBe(0);
  }

  public void Null_sorts_first() =>
    Color.Red.CompareTo(null).ShouldBeGreaterThan(0);

  public void Throws_ArgumentException_for_non_enumeration() =>
    Should.Throw<ArgumentException>(() => Color.Red.CompareTo("not an enumeration"));
}

public class Equals_And_GetHashCode
{
  public void Same_value_is_equal() =>
    Color.Red.Equals(Color.Red).ShouldBeTrue();

  public void Different_value_is_not_equal() =>
    Color.Red.Equals(Color.Blue).ShouldBeFalse();

  public void Different_type_is_not_equal() =>
    Color.Red.Equals("Red").ShouldBeFalse();

  public void Null_is_not_equal() =>
    Color.Red.Equals(null).ShouldBeFalse();

  public void Same_value_has_same_hash_code() =>
    Color.Red.GetHashCode().ShouldBe(Color.Red.GetHashCode());
}

public class ToStringTests
{
  public void Returns_name() =>
    Color.Green.ToString().ShouldBe("Green");
}

public class Constructor
{
  public void Sets_properties()
  {
    Color.Red.Value.ShouldBe(1);
    Color.Red.Name.ShouldBe("Red");
    Color.Red.AlternateCodes.ShouldBe(new[] { "R", "FF0000" });
  }

  public void AlternateCodes_defaults_to_empty_when_null() =>
    Color.Blue.AlternateCodes.Count.ShouldBe(0);
}
