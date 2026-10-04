namespace TimeWarp.Architecture.Foundation.Domain.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>A concrete <see cref="Enumeration"/> used to exercise the base class.</summary>
internal sealed class Color : Enumeration
{
  public static readonly Color Red = new(1, "Red", ["R", "FF0000"]);
  public static readonly Color Green = new(2, "Green", ["G"]);
  public static readonly Color Blue = new(3, "Blue", null);

  private Color(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

/// <summary>Two members share a Value — the member set is ambiguous.</summary>
internal sealed class DuplicateValue : Enumeration
{
  public static readonly DuplicateValue First = new(1, "First", null);
  public static readonly DuplicateValue Second = new(1, "Second", null);

  private DuplicateValue(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

/// <summary>One member's Name is another member's alternate code — FromString would be order-dependent.</summary>
internal sealed class NameCodeCollision : Enumeration
{
  public static readonly NameCodeCollision Alpha = new(1, "Alpha", ["Beta"]);
  public static readonly NameCodeCollision Beta = new(2, "Beta", null);

  private NameCodeCollision(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

/// <summary>Two members share an alternate code.</summary>
internal sealed class SharedCode : Enumeration
{
  public static readonly SharedCode Alpha = new(1, "Alpha", ["X"]);
  public static readonly SharedCode Beta = new(2, "Beta", ["X"]);

  private SharedCode(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

/// <summary>A member whose Name is also its own alternate code is not ambiguous.</summary>
internal sealed class SelfCode : Enumeration
{
  public static readonly SelfCode Alpha = new(1, "Alpha", ["Alpha", "A"]);

  private SelfCode(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

/// <summary>Same Value as <see cref="Color.Red"/> but a different type.</summary>
internal sealed class Shade : Enumeration
{
  public static readonly Shade Dark = new(1, "Dark", null);

  private Shade(int value, string name, IReadOnlyList<string>? alternateCodes)
    : base(value, name, alternateCodes) { }
}

/// <summary>A contract-shaped DTO carrying Enumeration members.</summary>
internal sealed record Swatch(Color Primary, Color? Accent, Dictionary<Color, int> Counts);

public class GetAll
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetAll>();

  public static Task Returns_all_static_fields()
  {
    Enumeration.GetAll<Color>().ToList().Count.ShouldBe(3);
    return Task.CompletedTask;
  }

  public static Task Returns_members_in_declaration_order()
  {
    Enumeration.GetAll<Color>().ShouldBe([Color.Red, Color.Green, Color.Blue]);
    return Task.CompletedTask;
  }

  public static Task Returns_the_cached_member_set()
  {
    Enumeration.GetAll<Color>().ShouldBeSameAs(Enumeration.GetAll<Color>());
    return Task.CompletedTask;
  }

  public static Task Result_cannot_be_mutated_through_a_cast()
  {
    (Enumeration.GetAll<Color>() as IList<Color>)!.IsReadOnly.ShouldBeTrue();
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

public class TryFrom
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryFrom>();

  public static Task TryFromValue_returns_member_or_false()
  {
    Enumeration.TryFromValue(2, out Color? green).ShouldBeTrue();
    green.ShouldBe(Color.Green);
    Enumeration.TryFromValue(99, out Color? missing).ShouldBeFalse();
    missing.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task TryFromName_is_ordinal()
  {
    Enumeration.TryFromName("Blue", out Color? blue).ShouldBeTrue();
    blue.ShouldBe(Color.Blue);
    Enumeration.TryFromName("blue", out Color? _).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task TryFromString_matches_name_or_code()
  {
    Enumeration.TryFromString("R", out Color? red).ShouldBeTrue();
    red.ShouldBe(Color.Red);
    Enumeration.TryFromString("nope", out Color? _).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class Ambiguity
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Ambiguity>();

  public static Task Duplicate_value_throws()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.FromValue<DuplicateValue>(1))
      .Message.ShouldContain("value 1");
    return Task.CompletedTask;
  }

  public static Task Name_equal_to_another_members_code_throws()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.FromString<NameCodeCollision>("Beta"))
      .Message.ShouldContain("'Beta'");
    return Task.CompletedTask;
  }

  public static Task Shared_alternate_code_throws()
  {
    Should.Throw<InvalidOperationException>(() => Enumeration.GetAll<SharedCode>().ToList())
      .Message.ShouldContain("alternate code 'X'");
    return Task.CompletedTask;
  }

  public static Task Name_equal_to_own_code_is_allowed()
  {
    Enumeration.FromString<SelfCode>("Alpha").ShouldBe(SelfCode.Alpha);
    Enumeration.FromString<SelfCode>("A").ShouldBe(SelfCode.Alpha);
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

public class Comparison_Operators
{
  internal static Color? Missing() => Enumeration.TryFromValue(99, out Color? missing) ? missing : null;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Comparison_Operators>();

  public static Task Generic_CompareTo_orders_by_value()
  {
    IComparable<Enumeration> red = Color.Red;
    red.CompareTo(Color.Blue).ShouldBeLessThan(0);
    red.CompareTo(null).ShouldBeGreaterThan(0);
    return Task.CompletedTask;
  }

  public static Task Relational_operators_order_by_value()
  {
    (Color.Red < Color.Blue).ShouldBeTrue();
    (Color.Blue > Color.Red).ShouldBeTrue();
    (Color.Red <= Enumeration.FromValue<Color>(1)).ShouldBeTrue();
    (Color.Red >= Color.Blue).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Null_sorts_first()
  {
    Color? none = Missing();
    Color? alsoNone = Missing();
    (none < Color.Red).ShouldBeTrue();
    (Color.Red > none).ShouldBeTrue();
    (none <= alsoNone).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Sorts_with_default_comparer()
  {
    new[] { Color.Blue, Color.Red, Color.Green }.Order().ShouldBe([Color.Red, Color.Green, Color.Blue]);
    return Task.CompletedTask;
  }
}

public class Equality_Operators
{
  private static Color? Missing() => Comparison_Operators.Missing();

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Equality_Operators>();

  public static Task Same_member_is_equal()
  {
    (Color.Red == Enumeration.FromValue<Color>(1)).ShouldBeTrue();
    (Color.Red != Color.Blue).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Same_value_different_type_is_not_equal()
  {
    Enumeration red = Color.Red;
    Enumeration dark = Shade.Dark;
    (red == dark).ShouldBeFalse();
    red.Equals(dark).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Null_handling()
  {
    Color? none = Missing();
    Color? alsoNone = Missing();
    (none == alsoNone).ShouldBeTrue();
    (Color.Red == none).ShouldBeFalse();
    (none != Color.Red).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task IEquatable_equals_by_value_and_type()
  {
    IEquatable<Enumeration> red = Color.Red;
    red.Equals(Color.Red).ShouldBeTrue();
    red.Equals(Shade.Dark).ShouldBeFalse();
    red.Equals(null).ShouldBeFalse();
    return Task.CompletedTask;
  }
}

public class Json_Round_Trip
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Json_Round_Trip>();

  private static readonly JsonSerializerOptions Options = new() { Converters = { new EnumerationJsonConverterFactory() } };

  public static Task Writes_member_name()
  {
    JsonSerializer.Serialize(Color.Green, Options).ShouldBe("\"Green\"");
    return Task.CompletedTask;
  }

  public static Task Round_trips_a_dto_including_null_and_dictionary_keys()
  {
    Swatch swatch = new(Color.Red, null, new Dictionary<Color, int> { [Color.Blue] = 3 });

    string json = JsonSerializer.Serialize(swatch, Options);
    json.ShouldBe("{\"Primary\":\"Red\",\"Accent\":null,\"Counts\":{\"Blue\":3}}");

    Swatch roundTripped = JsonSerializer.Deserialize<Swatch>(json, Options)!;
    roundTripped.Primary.ShouldBeSameAs(Color.Red);
    roundTripped.Accent.ShouldBeNull();
    roundTripped.Counts[Color.Blue].ShouldBe(3);
    return Task.CompletedTask;
  }

  public static Task Unknown_name_fails_closed()
  {
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Color>("\"Magenta\"", Options));
    return Task.CompletedTask;
  }

  public static Task Name_read_is_case_sensitive()
  {
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Color>("\"red\"", Options));
    return Task.CompletedTask;
  }

  public static Task Numeric_token_fails_closed()
  {
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Color>("1", Options));
    return Task.CompletedTask;
  }

  public static Task Object_token_fails_closed()
  {
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Color>("{\"Value\":1,\"Name\":\"Red\"}", Options));
    return Task.CompletedTask;
  }

  public static Task Unknown_dictionary_key_fails_closed()
  {
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Dictionary<Color, int>>("{\"Magenta\":1}", Options));
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
