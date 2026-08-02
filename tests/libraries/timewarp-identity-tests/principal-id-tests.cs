namespace PrincipalId_;

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

public class Mint
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Mint>();

  public static Task Returns_non_empty_id()
  {
    PrincipalId id = PrincipalId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Returns_distinct_ids()
  {
    PrincipalId a = PrincipalId.New();
    PrincipalId b = PrincipalId.New();
    a.ShouldNotBe(b);
    return Task.CompletedTask;
  }
}

public class From
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<From>();

  public static Task Accepts_non_empty_guid()
  {
    Guid value = Guid.CreateVersion7();
    PrincipalId id = PrincipalId.From(value);
    id.Value.ShouldBe(value);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_guid()
  {
    Should.Throw<ArgumentException>(() => PrincipalId.From(Guid.Empty));
    return Task.CompletedTask;
  }
}

public class Json
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Json>();

  public static Task Serializes_as_plain_guid_string()
  {
    PrincipalId id = PrincipalId.New();
    string json = JsonSerializer.Serialize(id);
    json.ShouldBe($"\"{id.Value}\"");
    json.ShouldNotContain("Value");
    json.ShouldNotContain("IsEmpty");
    return Task.CompletedTask;
  }

  public static Task Round_trips_new_id()
  {
    PrincipalId original = PrincipalId.New();
    string json = JsonSerializer.Serialize(original);
    PrincipalId restored = JsonSerializer.Deserialize<PrincipalId>(json);
    restored.ShouldBe(original);
    return Task.CompletedTask;
  }

  public static Task Deserialize_empty_string_throws()
  {
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PrincipalId>("\"\""));
    return Task.CompletedTask;
  }

  public static Task Deserialize_empty_guid_throws()
  {
    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<PrincipalId>($"\"{Guid.Empty}\""));
    return Task.CompletedTask;
  }
}

public class Parse
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Parse>();

  public static Task Parse_round_trips_string()
  {
    PrincipalId original = PrincipalId.New();
    PrincipalId parsed = PrincipalId.Parse(original.ToString(), provider: null);
    parsed.ShouldBe(original);
    return Task.CompletedTask;
  }

  public static Task TryParse_accepts_valid()
  {
    PrincipalId original = PrincipalId.New();
    PrincipalId.TryParse(original.ToString(), provider: null, out PrincipalId result).ShouldBeTrue();
    result.ShouldBe(original);
    return Task.CompletedTask;
  }

  public static Task TryParse_rejects_empty_guid_string()
  {
    PrincipalId.TryParse(Guid.Empty.ToString(), provider: null, out PrincipalId result).ShouldBeFalse();
    result.IsEmpty.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Parse_rejects_garbage()
  {
    Should.Throw<FormatException>(() => PrincipalId.Parse("not-a-guid", provider: null));
    return Task.CompletedTask;
  }
}

public class TypeConversion
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TypeConversion>();

  public static Task Converts_from_and_to_string()
  {
    PrincipalId original = PrincipalId.New();
    TypeConverter converter = TypeDescriptor.GetConverter(typeof(PrincipalId));
    converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
    converter.CanConvertTo(typeof(string)).ShouldBeTrue();

    object? fromString = converter.ConvertFrom(null, CultureInfo.InvariantCulture, original.ToString());
    fromString.ShouldBe(original);

    object? asString = converter.ConvertTo(null, CultureInfo.InvariantCulture, original, typeof(string));
    asString.ShouldBe(original.ToString());
    return Task.CompletedTask;
  }

  public static Task Converts_from_and_to_guid()
  {
    PrincipalId original = PrincipalId.New();
    TypeConverter converter = TypeDescriptor.GetConverter(typeof(PrincipalId));

    object? fromGuid = converter.ConvertFrom(null, CultureInfo.InvariantCulture, original.Value);
    fromGuid.ShouldBe(original);

    object? asGuid = converter.ConvertTo(null, CultureInfo.InvariantCulture, original, typeof(Guid));
    asGuid.ShouldBe(original.Value);
    return Task.CompletedTask;
  }
}

public class Compare
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Compare>();

  public static Task Orders_by_underlying_guid()
  {
    Guid low = Guid.Parse("00000000-0000-0000-0000-000000000001");
    Guid high = Guid.Parse("00000000-0000-0000-0000-000000000002");
    PrincipalId a = PrincipalId.From(low);
    PrincipalId b = PrincipalId.From(high);

    a.CompareTo(b).ShouldBeLessThan(0);
    b.CompareTo(a).ShouldBeGreaterThan(0);
    a.CompareTo(a).ShouldBe(0);
    return Task.CompletedTask;
  }
}
