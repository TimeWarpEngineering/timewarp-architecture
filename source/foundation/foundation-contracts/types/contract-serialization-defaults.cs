#region Purpose
// Single authority for the JSON serializer options used across the contract seam.
#endregion

#region Design
// The contract seam (SPA client, mock services, test harnesses, contract round-trip tests) must
// serialize identically or drift silently: camelCase JSON matching ASP.NET Core's Web defaults on
// the server side. Declaring the options once here removes the copies that previously agreed only
// by convention. Options is a shared instance (System.Text.Json freezes options on first use;
// no seam participant mutates them); Apply targets DI's Configure<JsonSerializerOptions> pattern.
#endregion

namespace TimeWarp.Foundation.Types;

public static class ContractSerializationDefaults
{
  /// <summary>The canonical contract-seam serializer options (camelCase, matching server Web defaults).</summary>
  public static JsonSerializerOptions Options { get; } = CreateOptions();

  /// <summary>Applies the canonical settings to an existing instance (DI <c>Configure</c> pattern).</summary>
  public static void Apply(JsonSerializerOptions options)
  {
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
  }

  private static JsonSerializerOptions CreateOptions()
  {
    JsonSerializerOptions options = new();
    Apply(options);
    return options;
  }
}
