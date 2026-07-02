#region Purpose
// RFC 7807 problem-details type usable by contracts and the WASM client.
#endregion

#region Design
// Duplicates Microsoft.AspNetCore.Mvc.ProblemDetails because contracts must not reference
// ASP.NET Core: this assembly is shared with the browser client and gRPC services.
// Serialization attributes mirror the framework type exactly (property order, null omission,
// extension-data catch-all) so payloads written by server ProblemDetails deserialize here
// losslessly — the "errors" dictionary of validation responses lands in Extensions.
#endregion

namespace TimeWarp.Foundation.Types;

public sealed class SharedProblemDetails
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-5)]
    public string? Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-4)]
    public string? Title { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-3)]
    public int? Status { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-2)]
    public string? Detail { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-1)]
    public string? Instance { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
