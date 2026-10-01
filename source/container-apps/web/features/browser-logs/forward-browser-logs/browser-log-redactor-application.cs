#region Purpose
// Strips bearer tokens and JWT-shaped strings from browser log text before it is logged.
#endregion

#region Design
// Best-effort secret hygiene, not a security boundary: console output can contain Authorization
// headers or tokens from failed requests. Recognised: Bearer values, three-segment eyJ... strings,
// and the values of common secret query/fragment parameters (access_token, id_token,
// refresh_token, token, code, client_secret, password) — the key is kept, the value replaced.
#endregion

namespace TimeWarp.Architecture.Features.BrowserLogs.Application;

using System.Text.RegularExpressions;

public static partial class BrowserLogRedactor
{
  private const string Replacement = "[redacted]";

  public static string Redact(string message) =>
    JwtPattern().Replace(
      BearerPattern().Replace(
        SecretParameterPattern().Replace(message, "${key}=" + Replacement),
        Replacement),
      Replacement);

  [GeneratedRegex(@"(?<key>\b(?:access_token|id_token|refresh_token|token|code|client_secret|password))=[^&#\s""';]+", RegexOptions.IgnoreCase)]
  private static partial Regex SecretParameterPattern();

  [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
  private static partial Regex BearerPattern();

  [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*")]
  private static partial Regex JwtPattern();
}
