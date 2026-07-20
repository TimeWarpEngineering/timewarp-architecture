namespace PrincipalId_;

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

public class Mint
{
  public void Returns_non_empty_id()
  {
    PrincipalId id = PrincipalId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
  }

  public void Returns_distinct_ids()
  {
    PrincipalId a = PrincipalId.New();
    PrincipalId b = PrincipalId.New();
    a.ShouldNotBe(b);
  }
}

public class From
{
  public void Accepts_non_empty_guid()
  {
    Guid value = Guid.CreateVersion7();
    PrincipalId id = PrincipalId.From(value);
    id.Value.ShouldBe(value);
  }

  public void Rejects_empty_guid() =>
    Should.Throw<ArgumentException>(() => PrincipalId.From(Guid.Empty));
}

public class Json
{
  public void Serializes_as_plain_guid_string()
  {
    PrincipalId id = PrincipalId.New();
    string json = JsonSerializer.Serialize(id);
    json.ShouldBe($"\"{id.Value}\"");
    json.ShouldNotContain("Value");
    json.ShouldNotContain("IsEmpty");
  }

  public void Round_trips_new_id()
  {
    PrincipalId original = PrincipalId.New();
    string json = JsonSerializer.Serialize(original);
    PrincipalId restored = JsonSerializer.Deserialize<PrincipalId>(json);
    restored.ShouldBe(original);
  }

  public void Deserialize_empty_string_throws() =>
    Should.Throw<JsonException>(() => JsonSerializer.Deserialize<PrincipalId>("\"\""));

  public void Deserialize_empty_guid_throws() =>
    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<PrincipalId>($"\"{Guid.Empty}\""));
}

public class Parse
{
  public void Parse_round_trips_string()
  {
    PrincipalId original = PrincipalId.New();
    PrincipalId parsed = PrincipalId.Parse(original.ToString(), provider: null);
    parsed.ShouldBe(original);
  }

  public void TryParse_accepts_valid()
  {
    PrincipalId original = PrincipalId.New();
    PrincipalId.TryParse(original.ToString(), provider: null, out PrincipalId result).ShouldBeTrue();
    result.ShouldBe(original);
  }

  public void TryParse_rejects_empty_guid_string()
  {
    PrincipalId.TryParse(Guid.Empty.ToString(), provider: null, out PrincipalId result).ShouldBeFalse();
    result.IsEmpty.ShouldBeTrue();
  }

  public void Parse_rejects_garbage() =>
    Should.Throw<FormatException>(() => PrincipalId.Parse("not-a-guid", provider: null));
}

public class TypeConversion
{
  public void Converts_from_and_to_string()
  {
    PrincipalId original = PrincipalId.New();
    TypeConverter converter = TypeDescriptor.GetConverter(typeof(PrincipalId));
    converter.CanConvertFrom(typeof(string)).ShouldBeTrue();
    converter.CanConvertTo(typeof(string)).ShouldBeTrue();

    object? fromString = converter.ConvertFrom(null, CultureInfo.InvariantCulture, original.ToString());
    fromString.ShouldBe(original);

    object? asString = converter.ConvertTo(null, CultureInfo.InvariantCulture, original, typeof(string));
    asString.ShouldBe(original.ToString());
  }

  public void Converts_from_and_to_guid()
  {
    PrincipalId original = PrincipalId.New();
    TypeConverter converter = TypeDescriptor.GetConverter(typeof(PrincipalId));

    object? fromGuid = converter.ConvertFrom(null, CultureInfo.InvariantCulture, original.Value);
    fromGuid.ShouldBe(original);

    object? asGuid = converter.ConvertTo(null, CultureInfo.InvariantCulture, original, typeof(Guid));
    asGuid.ShouldBe(original.Value);
  }
}

public class Compare
{
  public void Orders_by_underlying_guid()
  {
    Guid low = Guid.Parse("00000000-0000-0000-0000-000000000001");
    Guid high = Guid.Parse("00000000-0000-0000-0000-000000000002");
    PrincipalId a = PrincipalId.From(low);
    PrincipalId b = PrincipalId.From(high);

    a.CompareTo(b).ShouldBeLessThan(0);
    b.CompareTo(a).ShouldBeGreaterThan(0);
    a.CompareTo(a).ShouldBe(0);
  }
}
