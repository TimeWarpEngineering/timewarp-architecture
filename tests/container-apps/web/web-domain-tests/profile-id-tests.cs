namespace ProfileId_;

using System.Text.Json;

public class Mint
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Mint>();

  public static Task Returns_non_empty_id()
  {
    ProfileId id = ProfileId.New();
    id.Value.ShouldNotBe(Guid.Empty);
    id.IsEmpty.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Returns_distinct_ids()
  {
    ProfileId a = ProfileId.New();
    ProfileId b = ProfileId.New();
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
    ProfileId id = ProfileId.From(value);
    id.Value.ShouldBe(value);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_guid()
  {
    Should.Throw<ArgumentException>(() => ProfileId.From(Guid.Empty));
    return Task.CompletedTask;
  }
}

public class Json
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Json>();

  public static Task Serializes_as_plain_guid_string()
  {
    ProfileId id = ProfileId.New();
    string json = JsonSerializer.Serialize(id);
    json.ShouldBe($"\"{id.Value}\"");
    return Task.CompletedTask;
  }

  public static Task Round_trips_new_id()
  {
    ProfileId original = ProfileId.New();
    string json = JsonSerializer.Serialize(original);
    ProfileId restored = JsonSerializer.Deserialize<ProfileId>(json);
    restored.ShouldBe(original);
    return Task.CompletedTask;
  }

  public static Task Deserialize_empty_guid_throws()
  {
    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<ProfileId>($"\"{Guid.Empty}\""));
    return Task.CompletedTask;
  }
}
