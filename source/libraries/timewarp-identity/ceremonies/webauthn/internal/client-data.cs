#region Purpose
// Structured parse of the WebAuthn clientDataJSON blob (UTF-8 JSON produced by the browser).
#endregion

#region Design
// Only the three fields this verifier actually checks are modeled (type/challenge/origin) — the
// spec's clientDataJSON may carry additional members (tokenBinding, clientExtensionResults, ...)
// that this template posture ignores entirely; unknown members are silently skipped by
// System.Text.Json's default behavior, not an error.
// TryParse fails closed: a null/absent value for any of the three modeled fields, or a JSON parse
// error, returns false rather than a partially-populated ClientData.
#endregion

namespace TimeWarp.Identity;

internal sealed class ClientData
{
  [JsonPropertyName("type")]
  public string? Type { get; init; }

  [JsonPropertyName("challenge")]
  public string? Challenge { get; init; }

  [JsonPropertyName("origin")]
  public string? Origin { get; init; }

  public static bool TryParse(byte[] clientDataJson, out ClientData? clientData)
  {
    try
    {
      ClientData? parsed = JsonSerializer.Deserialize<ClientData>(clientDataJson);
      if (parsed is { Type.Length: > 0, Challenge.Length: > 0, Origin.Length: > 0 })
      {
        clientData = parsed;
        return true;
      }

      clientData = null;
      return false;
    }
    catch (JsonException)
    {
      clientData = null;
      return false;
    }
  }
}
