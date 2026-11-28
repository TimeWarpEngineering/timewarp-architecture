#region Purpose
// Turns prefill arguments into a stored draft and copies that draft onto a working form.
#endregion

#region Design
// Only supplied properties are stored, so an omitted bool is not false. Unknown keys are
// ignored when at least one key matches. No match, including an empty object, is not a draft:
// the caller stores nothing and the error lists the accepted keys. Explicit null is kept
// only for a nullable property. Canonical JSON for a field
// is the contract serializer, so a later edit hides the agent mark.
// ArgumentsMustBeObjectError and NoFieldMatchedPrefix are the sentences AskDiagnostics
// codes as NotObject and NoMatch, so the two cannot drift apart.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Nodes;

partial class FormPrefill
{
  private static readonly JsonSerializerOptions Indented =
    new(ContractSerializationDefaults.Options) { WriteIndented = true };

  public static bool TryNormalize
  (
    Type commandType,
    string? argumentsJson,
    out string suggestionJson,
    out string? error,
    out IReadOnlyList<string> fields
  )
  {
    ArgumentNullException.ThrowIfNull(commandType);
    if (string.IsNullOrWhiteSpace(argumentsJson))
    {
      return NoMatch(commandType, out suggestionJson, out error, out fields);
    }

    JsonNode? node;
    try
    {
      node = JsonNode.Parse(argumentsJson);
    }
    catch (JsonException exception)
    {
      suggestionJson = "";
      error = exception.Message;
      fields = [];
      return false;
    }

    if (node is null)
    {
      return NoMatch(commandType, out suggestionJson, out error, out fields);
    }

    if (node is not JsonObject arguments)
    {
      suggestionJson = "";
      error = ArgumentsMustBeObjectError;
      fields = [];
      return false;
    }

    return TryNormalizeObject(commandType, arguments, out suggestionJson, out error, out fields);
  }

  public static bool TryNormalize
  (
    Type commandType,
    IEnumerable<KeyValuePair<string, object?>>? arguments,
    out string suggestionJson,
    out string? error,
    out IReadOnlyList<string> fields
  )
  {
    ArgumentNullException.ThrowIfNull(commandType);
    JsonObject node = [];
    if (arguments is not null)
    {
      foreach (KeyValuePair<string, object?> pair in arguments)
      {
        node[pair.Key] = AsNode(pair.Value);
      }
    }

    return TryNormalizeObject(commandType, node, out suggestionJson, out error, out fields);
  }

  public static string Render(string suggestionJson)
  {
    ArgumentNullException.ThrowIfNull(suggestionJson);
    var node = JsonNode.Parse(suggestionJson);
    return node is null ? suggestionJson : node.ToJsonString(Indented);
  }
  /// <summary>Copies the supplied fields onto <paramref name="target"/> and records their canonical JSON.</summary>
  public static IReadOnlyList<string> Apply
  (
    object target,
    string suggestionJson,
    IDictionary<string, string> filled
  )
  {
    ArgumentNullException.ThrowIfNull(target);
    ArgumentNullException.ThrowIfNull(suggestionJson);
    ArgumentNullException.ThrowIfNull(filled);

    var node = JsonNode.Parse(suggestionJson);
    if (node is not JsonObject arguments)
    {
      throw new InvalidOperationException("A stored draft must be a JSON object.");
    }

    Dictionary<string, PropertyInfo> properties = Properties(target.GetType());
    List<string> fields = [];
    foreach (KeyValuePair<string, JsonNode?> pair in arguments)
    {
      if (!properties.TryGetValue(pair.Key, out PropertyInfo? property))
      {
        continue;
      }

      // JsonNode.Parse stores JSON null as a null reference, not JsonValueKind.Null.
      object? value = pair.Value is null || pair.Value.GetValueKind() == JsonValueKind.Null
        ? null
        : pair.Value.Deserialize(property.PropertyType, ContractSerializationDefaults.Options);
      property.SetValue(target, value);
      filled[property.Name] = JsonSerializer.Serialize(value, property.PropertyType, ContractSerializationDefaults.Options);
      fields.Add(pair.Key);
    }

    return fields;
  }

  public static bool Matches(object target, string propertyName, string canonical)
  {
    ArgumentNullException.ThrowIfNull(target);
    ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
    ArgumentNullException.ThrowIfNull(canonical);
    PropertyInfo? property = target.GetType().GetProperty(propertyName);
    if (property?.GetMethod is null)
    {
      return false;
    }

    object? value = property.GetValue(target);
    return JsonSerializer.Serialize(value, property.PropertyType, ContractSerializationDefaults.Options) == canonical;
  }

  private static bool TryNormalizeObject
  (
    Type commandType,
    JsonObject arguments,
    out string suggestionJson,
    out string? error,
    out IReadOnlyList<string> fields
  )
  {
    Dictionary<string, PropertyInfo> properties = Properties(commandType);
    NullabilityInfoContext nullability = new();
    JsonObject normalized = [];
    List<string> names = [];
    foreach (KeyValuePair<string, JsonNode?> pair in arguments)
    {
      if (!TryMatch(properties, pair.Key, out PropertyInfo? matched) || matched is null)
      {
        continue;
      }

      JsonNode? value = pair.Value;
      if (!ValueFits(matched, value, nullability, out error))
      {
        suggestionJson = "";
        fields = [];
        return false;
      }

      string camel = JsonNamingPolicy.CamelCase.ConvertName(matched.Name);
      // A null node is JSON null. Assigning it keeps the property.
      normalized[camel] = value?.DeepClone();
      names.Add(camel);
    }

    if (names.Count == 0)
    {
      return NoMatch(commandType, out suggestionJson, out error, out fields);
    }

    suggestionJson = normalized.ToJsonString();
    error = null;
    fields = names;
    return true;
  }

  private static bool NoMatch(Type commandType, out string suggestionJson, out string? error, out IReadOnlyList<string> fields)
  {
    suggestionJson = "";
    List<string> keys = [.. Properties(commandType).Keys];
    keys.Sort(StringComparer.Ordinal);
    error = NoFieldMatchedPrefix + " Accepted keys: " + string.Join(", ", keys) + ".";
    fields = [];
    return false;
  }

  private static Dictionary<string, PropertyInfo> Properties(Type type)
  {
    Dictionary<string, PropertyInfo> properties = new(StringComparer.Ordinal);
    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
      if (property.GetMethod is null || property.GetMethod.GetParameters().Length > 0 || property.SetMethod is null)
      {
        continue;
      }

      properties[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = property;
    }

    return properties;
  }

  private static bool TryMatch
  (
    Dictionary<string, PropertyInfo> properties,
    string key,
    out PropertyInfo? property
  )
  {
    if (properties.TryGetValue(key, out property))
    {
      return true;
    }

    PropertyInfo? found = null;
    foreach (KeyValuePair<string, PropertyInfo> pair in properties)
    {
      if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if (found is not null)
      {
        property = null;
        return false;
      }

      found = pair.Value;
    }

    property = found;
    return found is not null;
  }

  private static bool ValueFits
  (
    PropertyInfo property,
    JsonNode? node,
    NullabilityInfoContext nullability,
    out string? error
  )
  {
    string camel = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    if (node is null || node.GetValueKind() == JsonValueKind.Null)
    {
      if (!IsNullable(property, nullability))
      {
        error = camel + " cannot be null.";
        return false;
      }

      error = null;
      return true;
    }

    Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
    JsonValueKind kind = node.GetValueKind();
    if (type == typeof(string))
    {
      if (kind != JsonValueKind.String)
      {
        error = camel + " must be a string.";
        return false;
      }
    }
    else if (type == typeof(bool))
    {
      if (kind is not JsonValueKind.True and not JsonValueKind.False)
      {
        error = camel + " must be a boolean.";
        return false;
      }
    }
    else if (type.IsEnum || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
    {
      if (kind != JsonValueKind.String)
      {
        error = camel + " must be a string.";
        return false;
      }
    }
    else if (IsInteger(type))
    {
      if (kind != JsonValueKind.Number)
      {
        error = camel + " must be an integer.";
        return false;
      }
    }
    else if (IsNumber(type))
    {
      if (kind != JsonValueKind.Number)
      {
        error = camel + " must be a number.";
        return false;
      }
    }

    try
    {
      _ = node.Deserialize(property.PropertyType, ContractSerializationDefaults.Options);
    }
    catch (JsonException)
    {
      error = camel + " is not a valid " + type.Name + ".";
      return false;
    }

    error = null;
    return true;
  }

  private static bool IsNullable(PropertyInfo property, NullabilityInfoContext nullability)
  {
    if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
    {
      return true;
    }

    if (property.PropertyType.IsValueType)
    {
      return false;
    }

    return nullability.Create(property).WriteState == NullabilityState.Nullable;
  }

  private static bool IsInteger(Type type) =>
    type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
    || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);

  private static bool IsNumber(Type type) =>
    type == typeof(float) || type == typeof(double) || type == typeof(decimal);

  private static JsonNode? AsNode(object? value)
  {
    if (value is null)
    {
      return null;
    }

    if (value is JsonElement element)
    {
      return JsonNode.Parse(element.GetRawText());
    }

    if (value is JsonNode node)
    {
      return node.DeepClone();
    }

    return JsonSerializer.SerializeToNode(value, ContractSerializationDefaults.Options);
  }
}
