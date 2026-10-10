#region Purpose
// Renders a tool-call argument dictionary as JSON for the Ask panel.
#endregion

#region Design
// Razor prints a dictionary with Object.ToString, which is the CLR type name
// (System.Collections.Generic.Dictionary`2[...]). page_context's schema is empty, so the
// arguments that reach the panel are {}. That payload is noise and used to be the type name.
// Format returns null for null or empty arguments so the panel omits the pre. A non-empty
// dictionary is copied first: the runtime value may be a dictionary subtype whose own
// serializer would walk services or other non-argument members. JsonElement values, which
// the relay stores after ArgumentsJson is deserialized, serialize as JSON.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>JSON text for tool arguments shown in Ask.</summary>
public static class AgentArgumentText
{
  private static readonly JsonSerializerOptions Indented = new()
  {
    WriteIndented = true,
  };

  public static string? Format(IDictionary<string, object?>? arguments)
  {
    if (arguments is null || arguments.Count == 0)
    {
      return null;
    }

    Dictionary<string, object?> copy = [];
    foreach (KeyValuePair<string, object?> pair in arguments)
    {
      copy[pair.Key] = pair.Value;
    }

    return JsonSerializer.Serialize(copy, Indented);
  }
}
