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
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetAll>();

  public static Task Returns_all_static_fields()
  {
    Enumeration.GetAll<Color>().ToList().Count.ShouldBe(3);
    return Task.CompletedTask;
  }
}

public class FromValue
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FromValue>();

  public static Task Returns_match_for_valid_value()
  {
    Enumeration.FromValue<Color>(1).ShouldBe(Color.Red);
    return Task.CompletedTask;
  }

  public static Task Throws_InvalidOperationException_for_invalid_value()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.FromValue<Color>(99));
    return Task.CompletedTask;
  }
}

public class FromName
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FromName>();

  public static Task Returns_match_for_valid_name()
  {
    Enumeration.FromName<Color>("Green").ShouldBe(Color.Green);
    return Task.CompletedTask;
  }

  public static Task Throws_InvalidOperationException_for_invalid_name()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.FromName<Color>("Magenta"));
    return Task.CompletedTask;
  }
}

public class FromAlternateCode
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FromAlternateCode>();

  public static Task Returns_match_for_valid_code()
  {
    Enumeration.FromAlternateCode<Color>("FF0000").ShouldBe(Color.Red);
    return Task.CompletedTask;
  }

  public static Task Throws_InvalidOperationException_for_invalid_code()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.FromAlternateCode<Color>("ZZ"));
    return Task.CompletedTask;
  }
}

public class FromString
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FromString>();

  public static Task Returns_match_by_name()
  {
    Enumeration.FromString<Color>("Blue").ShouldBe(Color.Blue);
    return Task.CompletedTask;
  }

  public static Task Returns_match_by_alternate_code()
  {
    Enumeration.FromString<Color>("G").ShouldBe(Color.Green);
    return Task.CompletedTask;
  }

  public static Task Throws_InvalidOperationException_for_invalid_input()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.FromString<Color>("nope"));
    return Task.CompletedTask;
  }
}

public class CompareTo
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompareTo>();

  public static Task Orders_by_value()
  {
    Color.Red.CompareTo(Color.Blue).ShouldBeLessThan(0);
    Color.Blue.CompareTo(Color.Red).ShouldBeGreaterThan(0);
    Color.Red.CompareTo(Color.Red).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Null_sorts_first()
  {
    Color.Red.CompareTo(null).ShouldBeGreaterThan(0);
    return Task.CompletedTask;
  }

  public static Task Throws_ArgumentException_for_non_enumeration()
  {
    Should.Throw<ArgumentException>(() => Color.Red.CompareTo("not an enumeration"));
    return Task.CompletedTask;
  }
}

public class Equals_And_GetHashCode
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Equals_And_GetHashCode>();

  public static Task Same_value_is_equal()
  {
    Color.Red.Equals(Color.Red).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Different_value_is_not_equal()
  {
    Color.Red.Equals(Color.Blue).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Different_type_is_not_equal()
  {
    Color.Red.Equals("Red").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Null_is_not_equal()
  {
    Color.Red.Equals(null).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Same_value_has_same_hash_code()
  {
    Color.Red.GetHashCode().ShouldBe(Color.Red.GetHashCode());
    return Task.CompletedTask;
  }
}

public class ToStringTests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ToStringTests>();

  public static Task Returns_name()
  {
    Color.Green.ToString().ShouldBe("Green");
    return Task.CompletedTask;
  }
}

public class Constructor
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Constructor>();

  public static Task Sets_properties()
  {
    Color.Red.Value.ShouldBe(1);
    Color.Red.Name.ShouldBe("Red");
    Color.Red.AlternateCodes.ShouldBe(new[] { "R", "FF0000" });
    return Task.CompletedTask;
  }

  public static Task AlternateCodes_defaults_to_empty_when_null()
  {
    Color.Blue.AlternateCodes.Count.ShouldBe(0);
    return Task.CompletedTask;
  }
}
