namespace CredentialId_;

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

public class Mint
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Mint>();

  public static Task Returns_non_empty_id()
  {
    CredentialId id = CredentialId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Returns_distinct_ids()
  {
    CredentialId a = CredentialId.New();
    CredentialId b = CredentialId.New();
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
    CredentialId id = CredentialId.From(value);
    id.Value.ShouldBe(value);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_guid()
  {
    Should.Throw<ArgumentException>(() => CredentialId.From(Guid.Empty));
    return Task.CompletedTask;
  }
}

public class Json
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Json>();

  public static Task Serializes_as_plain_guid_string()
  {
    CredentialId id = CredentialId.New();
    string json = JsonSerializer.Serialize(id);
    json.ShouldBe($"\"{id.Value}\"");
    json.ShouldNotContain("Value");
    return Task.CompletedTask;
  }

  public static Task Round_trips_new_id()
  {
    CredentialId original = CredentialId.New();
    string json = JsonSerializer.Serialize(original);
    CredentialId restored = JsonSerializer.Deserialize<CredentialId>(json);
    restored.ShouldBe(original);
    return Task.CompletedTask;
  }

  public static Task Deserialize_empty_guid_throws()
  {
    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<CredentialId>($"\"{Guid.Empty}\""));
    return Task.CompletedTask;
  }
}

public class Parse
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Parse>();

  public static Task Parse_and_try_parse_round_trip()
  {
    CredentialId original = CredentialId.New();
    CredentialId.Parse(original.ToString(), provider: null).ShouldBe(original);
    CredentialId.TryParse(original.ToString(), provider: null, out CredentialId result).ShouldBeTrue();
    result.ShouldBe(original);
    return Task.CompletedTask;
  }
}

public class TypeConversion
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TypeConversion>();

  public static Task Converts_from_string()
  {
    CredentialId original = CredentialId.New();
    TypeConverter converter = TypeDescriptor.GetConverter(typeof(CredentialId));
    converter.ConvertFrom(null, CultureInfo.InvariantCulture, original.ToString()).ShouldBe(original);
    return Task.CompletedTask;
  }
}

public class Compare
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Compare>();

  public static Task Orders_by_underlying_guid()
  {
    CredentialId a = CredentialId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    CredentialId b = CredentialId.From(Guid.Parse("00000000-0000-0000-0000-000000000002"));
    a.CompareTo(b).ShouldBeLessThan(0);
    return Task.CompletedTask;
  }
}
