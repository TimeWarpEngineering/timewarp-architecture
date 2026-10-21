#region Purpose
// Checks the shape of an aca-neon Neon connection string without repeating the string in a refusal.
#endregion

#region Design
// Task 317: the pooled string (web-server) and the direct string (migrations) are operator
// secrets. This helper only reports which knob is wrong. It never includes the value, so a
// refusal can be printed. dev-cli does not reference Npgsql; the parser splits on ';' and the
// first '=' and compares keys with spaces, hyphens and underscores removed.
// Both strings need a host, SSL Mode Require or VerifyFull, and Timeout of at least 30 seconds
// (Neon Free can take that long to wake). The host key is Host or Server. A postgresql:// URI is
// refused. The pooled host must contain "-pooler" and must disable prepared statements,
// reset-on-close and channel binding, which PgBouncer transaction mode rejects. Channel Binding
// must be Disable; Npgsql rejects Disabled. Keepalive above 0 would ping Neon and burn the free
// compute hours, so it is rejected on the pooled string. The direct host must not be the pooler.
// A semicolon inside a password splits the string; Neon passwords do not need one.
#endregion

namespace DevCli.Services;

using System.Globalization;

/// <summary>Shape checks for the Neon connection strings <c>dev deploy --target aca-neon</c> requires.</summary>
internal static class NeonConnection
{
  internal const int MinimumTimeoutSeconds = 30;

  /// <summary>
  /// Problems with <paramref name="connectionString"/>. <paramref name="parameterName"/> is the only
  /// text from the caller that the messages include.
  /// </summary>
  internal static string[] Problems(string parameterName, string connectionString, bool pooled)
  {
    if (string.IsNullOrWhiteSpace(connectionString))
    {
      return [$"Neon connection string '{parameterName}' is empty."];
    }

    if (connectionString.Contains("://", StringComparison.Ordinal))
    {
      string kindName = pooled ? "pooled" : "direct";
      return [$"Neon {kindName} connection string '{parameterName}' must be the key/value form (Host or Server), not a URI."];
    }

    Dictionary<string, string> pairs = Parse(connectionString);
    List<string> problems = [];
    string kind = pooled ? "pooled" : "direct";
    string? host = ReadHost(pairs);

    if (host is null)
    {
      problems.Add($"Neon {kind} connection string '{parameterName}' must include a Host or Server.");
    }
    else if (pooled && !host.Contains("-pooler", StringComparison.OrdinalIgnoreCase))
    {
      problems.Add($"Neon pooled connection string '{parameterName}' must use the pooler host (the host contains \"-pooler\").");
    }
    else if (!pooled && host.Contains("-pooler", StringComparison.OrdinalIgnoreCase))
    {
      problems.Add($"Neon direct connection string '{parameterName}' must not use the pooler host.");
    }

    if (!pairs.TryGetValue("sslmode", out string? sslMode) || !IsRequiredSsl(sslMode))
    {
      problems.Add($"Neon {kind} connection string '{parameterName}' must set SSL Mode to Require or VerifyFull.");
    }

    if (!TryReadTimeout(pairs, out int timeoutSeconds) || timeoutSeconds < MinimumTimeoutSeconds)
    {
      problems.Add($"Neon {kind} connection string '{parameterName}' must set Timeout to at least {MinimumTimeoutSeconds} seconds (Neon Free may be waking).");
    }

    if (pooled)
    {
      if (!pairs.TryGetValue("maxautoprepare", out string? maxAutoPrepare) || maxAutoPrepare != "0")
      {
        problems.Add($"Neon pooled connection string '{parameterName}' must set Max Auto Prepare to 0 (PgBouncer transaction mode).");
      }

      if (!pairs.TryGetValue("noresetonclose", out string? noReset) || !IsTrue(noReset))
      {
        problems.Add($"Neon pooled connection string '{parameterName}' must set No Reset On Close to true.");
      }

      if (!pairs.TryGetValue("channelbinding", out string? channelBinding) || !IsChannelBindingDisabled(channelBinding))
      {
        problems.Add($"Neon pooled connection string '{parameterName}' must set Channel Binding to Disable.");
      }

      if (pairs.TryGetValue("keepalive", out string? keepalive) && keepalive != "0")
      {
        problems.Add($"Neon pooled connection string '{parameterName}' must not set Keepalive above 0 (a ping would keep Neon awake).");
      }
    }

    return [.. problems];
  }

  private static Dictionary<string, string> Parse(string connectionString)
  {
    Dictionary<string, string> pairs = new(StringComparer.Ordinal);
    foreach (string segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      int separator = segment.IndexOf('=', StringComparison.Ordinal);
      if (separator <= 0)
      {
        continue;
      }

      string key = NormalizeKey(segment[..separator]);
      if (key.Length == 0)
      {
        continue;
      }

      pairs[key] = segment[(separator + 1)..].Trim();
    }

    return pairs;
  }

  private static string NormalizeKey(string key)
  {
    Span<char> buffer = stackalloc char[key.Length];
    int length = 0;
    foreach (char character in key)
    {
      if (character is ' ' or '_' or '-')
      {
        continue;
      }

      buffer[length++] = char.ToLowerInvariant(character);
    }

    return new string(buffer[..length]);
  }

  private static bool TryReadTimeout(Dictionary<string, string> pairs, out int timeoutSeconds)
  {
    string? text = null;
    if (pairs.TryGetValue("timeout", out string? timeout))
    {
      text = timeout;
    }

    if (pairs.TryGetValue("connectiontimeout", out string? connectionTimeout))
    {
      text = connectionTimeout;
    }

    return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeoutSeconds);
  }

  private static bool IsRequiredSsl(string sslMode) =>
    sslMode.Equals("Require", StringComparison.OrdinalIgnoreCase)
    || sslMode.Equals("VerifyFull", StringComparison.OrdinalIgnoreCase);

  private static bool IsTrue(string value) =>
    value.Equals("true", StringComparison.OrdinalIgnoreCase)
    || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
    || value == "1";

  private static bool IsChannelBindingDisabled(string value) =>
    value.Equals("Disable", StringComparison.OrdinalIgnoreCase);

  /// <summary>Npgsql accepts Host and Server. Host wins when both are set.</summary>
  private static string? ReadHost(Dictionary<string, string> pairs)
  {
    if (pairs.TryGetValue("host", out string? host) && host.Length > 0)
    {
      return host;
    }

    if (pairs.TryGetValue("server", out string? server) && server.Length > 0)
    {
      return server;
    }

    return null;
  }
}
