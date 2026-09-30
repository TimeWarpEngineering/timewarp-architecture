#region Purpose
// Strips bearer tokens and JWT-shaped strings from browser log text before it is logged.
#endregion

#region Design
// Best-effort secret hygiene, not a security boundary: console output can contain Authorization
// headers or tokens from failed requests. Only Bearer values and three-segment eyJ... strings are
// recognised.
#endregion

namespace TimeWarp.Architecture.Features.BrowserLogs.Application;

using System.Text.RegularExpressions;

public static partial class BrowserLogRedactor
{
  private const string Replacement = "[redacted]";

  public static string Redact(string message) =>
    JwtPattern().Replace(BearerPattern().Replace(message, Replacement), Replacement);

  [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
  private static partial Regex BearerPattern();

  [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*")]
  private static partial Regex JwtPattern();
}
