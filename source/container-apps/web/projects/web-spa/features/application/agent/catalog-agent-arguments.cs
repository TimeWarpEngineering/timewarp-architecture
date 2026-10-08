#region Purpose
// Binds a tool-call argument object onto the positional array ActionCatalogEntry.Execute expects.
#endregion

#region Design
// Names match ActionCatalogParameter.Name, case-insensitive. JsonElement and other JSON values
// become the parameter's CLR type so ActionCatalogArguments.Get receives a real instance:
// it converts scalars, and it does not deserialize arbitrary complex objects.
// Missing optional trailing arguments are omitted. UserId is not filled here; handlers stamp it.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Converts a tool-call argument dictionary into catalog constructor arguments.</summary>
public static class CatalogAgentArguments
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

  public static object?[] Bind(ActionCatalogEntry entry, IReadOnlyDictionary<string, object?>? arguments)
  {
    ArgumentNullException.ThrowIfNull(entry);
    IReadOnlyDictionary<string, object?> values = arguments ?? new Dictionary<string, object?>();
    object?[] bound = new object?[entry.Parameters.Count];
    int lastProvided = -1;

    for (int index = 0; index < entry.Parameters.Count; index++)
    {
      ActionCatalogParameter parameter = entry.Parameters[index];
      if (!TryGet(values, parameter.Name, out object? raw))
      {
        if (parameter.IsRequired)
        {
          throw new ArgumentException($"Action '{entry.Name}' is missing argument '{parameter.Name}'.");
        }

        continue;
      }

      bound[index] = ConvertValue(raw, parameter.ClrType);
      lastProvided = index;
    }

    if (lastProvided < entry.Parameters.Count - 1)
    {
      object?[] trimmed = new object?[lastProvided + 1];
      Array.Copy(bound, trimmed, trimmed.Length);
      return trimmed;
    }

    return bound;
  }

  private static bool TryGet(IReadOnlyDictionary<string, object?> arguments, string name, out object? value)
  {
    if (arguments.TryGetValue(name, out value))
    {
      return true;
    }

    foreach (KeyValuePair<string, object?> pair in arguments)
    {
      if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
      {
        value = pair.Value;
        return true;
      }
    }

    value = null;
    return false;
  }

  private static object? ConvertValue(object? raw, Type clrType)
  {
    if (raw is null)
    {
      return null;
    }

    Type target = Nullable.GetUnderlyingType(clrType) ?? clrType;
    if (target.IsInstanceOfType(raw))
    {
      return raw;
    }

    if (raw is JsonElement element)
    {
      if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
      {
        return null;
      }

      return element.Deserialize(clrType, SerializerOptions);
    }

    string json = JsonSerializer.Serialize(raw, SerializerOptions);
    return JsonSerializer.Deserialize(json, clrType, SerializerOptions);
  }
}
