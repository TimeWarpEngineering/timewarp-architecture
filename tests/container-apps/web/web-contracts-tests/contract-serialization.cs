#region Purpose
// Single authority for the serializer options and round-trip helper used by every contract test.
#endregion

#region Design
// Options come from ContractSerializationDefaults — the single authority the SPA client and test
// harnesses share — so these tests exercise the exact seam the app uses.
// Trivial auto-property POCO round-trips are deliberately NOT written here: they cannot fail
// under default System.Text.Json. Tests target shapes where serialization can actually diverge —
// parameterized ctors with Guard clauses, ListResponse<T> envelopes, source-generated route
// properties, and SharedProblemDetails extension-data losslessness.
#endregion

namespace TimeWarp.Architecture.Web.Contracts.Tests;

internal static class ContractSerialization
{
  public static readonly JsonSerializerOptions Options = TimeWarp.Foundation.Types.ContractSerializationDefaults.Options;

  public static T RoundTrip<T>(T value) where T : class
  {
    string json = JsonSerializer.Serialize(value, Options);
    T? parsed = JsonSerializer.Deserialize<T>(json, Options);
    parsed.ShouldNotBeNull();
    return parsed;
  }
}
