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

/// <summary>
/// RFC 7807 problem details shared by contracts and the WASM client (no ASP.NET Core dependency).
/// </summary>
public sealed class SharedProblemDetails
{
    /// <summary>
    /// URI reference that identifies the problem type.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-5)]
    public string? Type { get; set; }

    /// <summary>
    /// Short, human-readable summary of the problem type.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-4)]
    public string? Title { get; set; }

    /// <summary>
    /// HTTP status code for this occurrence of the problem.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-3)]
    public int? Status { get; set; }

    /// <summary>
    /// Human-readable explanation specific to this occurrence.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-2)]
    public string? Detail { get; set; }

    /// <summary>
    /// URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(-1)]
    public string? Instance { get; set; }

    /// <summary>
    /// Extension members (for example validation <c>errors</c>) preserved across serialization.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
