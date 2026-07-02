#region Purpose
// Single authority for the serializer options and round-trip helper used by every contract test.
#endregion

#region Design
// Options mirror the SPA client's DI configuration (web-spa program.cs: CamelCase naming) — the
// seam these tests guard is "what the client writes is what the client reads back".
// Candidate improvement: hoist the canonical JsonSerializerOptions into foundation-contracts so
// the client, mocks, and these tests share one declaration instead of agreeing by convention.
// Trivial auto-property POCO round-trips are deliberately NOT written here: they cannot fail
// under default System.Text.Json. Tests target shapes where serialization can actually diverge —
// parameterized ctors with Guard clauses, ListResponse<T> envelopes, source-generated route
// properties, and SharedProblemDetails extension-data losslessness.
#endregion

namespace TimeWarp.Architecture.Web.Contracts.Tests;

internal static class ContractSerialization
{
  public static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public static T RoundTrip<T>(T value) where T : class
  {
    string json = JsonSerializer.Serialize(value, Options);
    T? parsed = JsonSerializer.Deserialize<T>(json, Options);
    parsed.ShouldNotBeNull();
    return parsed;
  }
}
