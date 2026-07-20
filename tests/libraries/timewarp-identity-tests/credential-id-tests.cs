namespace CredentialId_;

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

public class Mint
{
  public void Returns_non_empty_id()
  {
    CredentialId id = CredentialId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
  }

  public void Returns_distinct_ids()
  {
    CredentialId a = CredentialId.New();
    CredentialId b = CredentialId.New();
    a.ShouldNotBe(b);
  }
}

public class From
{
  public void Accepts_non_empty_guid()
  {
    Guid value = Guid.CreateVersion7();
    CredentialId id = CredentialId.From(value);
    id.Value.ShouldBe(value);
  }

  public void Rejects_empty_guid() =>
    Should.Throw<ArgumentException>(() => CredentialId.From(Guid.Empty));
}

public class Json
{
  public void Serializes_as_plain_guid_string()
  {
    CredentialId id = CredentialId.New();
    string json = JsonSerializer.Serialize(id);
    json.ShouldBe($"\"{id.Value}\"");
    json.ShouldNotContain("Value");
  }

  public void Round_trips_new_id()
  {
    CredentialId original = CredentialId.New();
    string json = JsonSerializer.Serialize(original);
    CredentialId restored = JsonSerializer.Deserialize<CredentialId>(json);
    restored.ShouldBe(original);
  }

  public void Deserialize_empty_guid_throws() =>
    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<CredentialId>($"\"{Guid.Empty}\""));
}

public class Parse
{
  public void Parse_and_try_parse_round_trip()
  {
    CredentialId original = CredentialId.New();
    CredentialId.Parse(original.ToString(), provider: null).ShouldBe(original);
    CredentialId.TryParse(original.ToString(), provider: null, out CredentialId result).ShouldBeTrue();
    result.ShouldBe(original);
  }
}

public class TypeConversion
{
  public void Converts_from_string()
  {
    CredentialId original = CredentialId.New();
    TypeConverter converter = TypeDescriptor.GetConverter(typeof(CredentialId));
    converter.ConvertFrom(null, CultureInfo.InvariantCulture, original.ToString()).ShouldBe(original);
  }
}

public class Compare
{
  public void Orders_by_underlying_guid()
  {
    CredentialId a = CredentialId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    CredentialId b = CredentialId.From(Guid.Parse("00000000-0000-0000-0000-000000000002"));
    a.CompareTo(b).ShouldBeLessThan(0);
  }
}
