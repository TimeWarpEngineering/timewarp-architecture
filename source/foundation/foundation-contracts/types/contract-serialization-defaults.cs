#region Purpose
// Single authority for the JSON serializer options used across the contract seam.
#endregion

#region Design
// The contract seam (SPA client, mock services, test harnesses, contract round-trip tests, and
// the server host via Apply on MVC JsonOptions + HttpJsonOptions) must serialize identically or
// drift silently: camelCase JSON property names matching ASP.NET Core's Web defaults.
//
// Enums on the wire are PascalCase member-name strings (JsonStringEnumConverter with namingPolicy
// null so member names write as declared; allowIntegerValues: false so integers and unknown
// strings fail closed with JsonException rather than mapping to 0/None). Read is case-insensitive
// (STJ default) — not a silent None map. Pure discriminators with no per-member behavior stay
// PLAIN C# enums: contract discriminators are plain C# enums. foundation-domain Enumeration is
// kept out of contracts so contracts carry no domain dependency (EnumerationJsonConverterFactory
// exists in foundation-domain but is NOT registered here; an app that needs it adds it to its own
// JsonSerializerOptions).
// Trigger: the first contract that genuinely needs an Enumeration - per-member behavior a plain
// enum cannot carry - triggers extracting Enumeration + its converter into a small standalone
// package that both contracts and domain reference, then registering the converter here (see
// task 105's extraction analysis).
//
// Declaring options once here removes copies that previously agreed only by convention. Options is
// a shared instance (System.Text.Json freezes options on first use; no seam participant mutates
// them); Apply targets DI's Configure pattern. Rejected alternative: keep integers + a TWA
// renumber-guard analyzer — more ceremony, less self-describing for agent consumers.
#endregion

namespace TimeWarp.Foundation.Types;

/// <summary>
/// Canonical <see cref="JsonSerializerOptions"/> for the contract seam (SPA, mocks, tests, and host JSON).
/// </summary>
public static class ContractSerializationDefaults
{
  /// <summary>The canonical contract-seam serializer options (camelCase properties; PascalCase string enums).</summary>
  public static JsonSerializerOptions Options { get; } = CreateOptions();

  /// <summary>Applies the canonical settings to an existing instance (DI <c>Configure</c> pattern).</summary>
  public static void Apply(JsonSerializerOptions options)
  {
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    if (!options.Converters.OfType<JsonStringEnumConverter>().Any())
    {
      options.Converters.Add(
        new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    }
  }

  private static JsonSerializerOptions CreateOptions()
  {
    JsonSerializerOptions options = new();
    Apply(options);
    return options;
  }
}
