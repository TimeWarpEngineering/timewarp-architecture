#region Purpose
// CamelCase System.Text.Json options + small helpers for CLI wire DTOs.
#endregion
#region Design
// Matches the contract seam (ContractSerializationDefaults): camelCase property names and
// PascalCase string enums (allowIntegerValues: false) so this client speaks the same JSON
// shape as web-contracts without referencing that assembly. Options are frozen — never
// mutated after construction.
#endregion

namespace AgentIdentityCli.Services;

internal sealed class CliJson
{
  public JsonSerializerOptions Options { get; } = CreateOptions();

  public string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

  public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

  private static JsonSerializerOptions CreateOptions()
  {
    JsonSerializerOptions options = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      PropertyNameCaseInsensitive = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      WriteIndented = false,
      TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };
    options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    return options;
  }
}
