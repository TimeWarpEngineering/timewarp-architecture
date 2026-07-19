namespace ProfileId_;

using System.Text.Json;

public class Mint
{
  public void Returns_non_empty_id()
  {
    ProfileId id = ProfileId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
  }

  public void Returns_distinct_ids()
  {
    ProfileId a = ProfileId.New();
    ProfileId b = ProfileId.New();
    a.ShouldNotBe(b);
  }
}

public class From
{
  public void Accepts_non_empty_guid()
  {
    Guid value = Guid.CreateVersion7();
    ProfileId id = ProfileId.From(value);
    id.Value.ShouldBe(value);
  }

  public void Rejects_empty_guid() =>
    Should.Throw<ArgumentException>(() => ProfileId.From(Guid.Empty));
}

public class Json
{
  public void Serializes_as_plain_guid_string()
  {
    ProfileId id = ProfileId.New();
    string json = JsonSerializer.Serialize(id);
    json.ShouldBe($"\"{id.Value}\"");
  }

  public void Round_trips_new_id()
  {
    ProfileId original = ProfileId.New();
    string json = JsonSerializer.Serialize(original);
    ProfileId restored = JsonSerializer.Deserialize<ProfileId>(json);
    restored.ShouldBe(original);
  }

  public void Deserialize_empty_guid_throws() =>
    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<ProfileId>($"\"{Guid.Empty}\""));
}
