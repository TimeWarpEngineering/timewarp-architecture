#region Purpose
// Binds a JSON object of named arguments to a catalog entry's positional object?[] (approach B of task 275).
#endregion

#region Design
// The catalog executes with positional arguments in constructor order; a server offer names them.
// Bind walks ActionCatalogEntry.Parameters in order: a present argument is deserialized to the
// parameter's ClrType with the contract-seam options (ContractSerializationDefaults), so the value
// handed to Execute is already that type and ActionCatalogArguments.Get passes it through untouched
// (Guid from "…", string, bool, records and dictionaries alike). Fail closed on anything off-shape:
// a missing required parameter (an explicit JSON null counts as missing), an argument name the entry does not declare, a value that does not
// deserialize, or an optional parameter skipped before a later one that is present (positional
// arrays cannot leave holes). A trailing run of absent optional parameters is simply not passed.
// Lives beside the palette (Applications) while the evaluation runs; it moves into TimeWarp.State
// only if approach B is adopted.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json;
using TimeWarp.Foundation.Types;

public static class ContextualActionArguments
{
  /// <summary>Parses a JSON object of arguments; null/empty text is an empty object.</summary>
  public static OneOf<Dictionary<string, JsonElement>, string> Parse(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
    }

    try
    {
      using var document = JsonDocument.Parse(json);
      if (document.RootElement.ValueKind != JsonValueKind.Object)
      {
        return "arguments are not a JSON object";
      }

      Dictionary<string, JsonElement> arguments = new(StringComparer.Ordinal);
      foreach (JsonProperty property in document.RootElement.EnumerateObject())
      {
        arguments[property.Name] = property.Value.Clone();
      }

      return arguments;
    }
    catch (JsonException)
    {
      return "arguments are not valid JSON";
    }
  }

  /// <summary>Positional arguments for <paramref name="entry"/>, or why binding failed.</summary>
  public static OneOf<object?[], string> Bind(ActionCatalogEntry entry, IReadOnlyDictionary<string, JsonElement> arguments)
  {
    foreach (string name in arguments.Keys)
    {
      if (!entry.Parameters.Any(parameter => parameter.Name == name))
      {
        return $"{entry.Name} has no parameter '{name}'";
      }
    }

    List<object?> positional = [];
    int skippedOptional = 0;
    foreach (ActionCatalogParameter parameter in entry.Parameters)
    {
      if (!arguments.TryGetValue(parameter.Name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
      {
        if (parameter.IsRequired)
        {
          return $"{entry.Name} needs '{parameter.Name}'";
        }

        skippedOptional++;
        continue;
      }

      if (skippedOptional > 0)
      {
        return $"{entry.Name} cannot bind '{parameter.Name}' after an omitted optional parameter";
      }

      try
      {
        positional.Add(value.Deserialize(parameter.ClrType, ContractSerializationDefaults.Options));
      }
      catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException or InvalidOperationException)
      {
        return $"{entry.Name} cannot bind '{parameter.Name}' as {parameter.ClrType.Name}";
      }
    }

    return positional.ToArray();
  }
}
