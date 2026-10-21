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
// Task 393: the pooled host's AWS region (the label before `.aws.neon.tech`) is compared with
// Azure:Location. Each AWS region has a small set of nearby Azure regions. The first is the one
// a warning names. A location in the set stays quiet. A location outside it is far. The warning
// never includes the connection string or the endpoint id.
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

  /// <summary>
  /// Warning when <paramref name="connectionString"/>'s AWS region is far from <paramref name="azureLocation"/>,
  /// or when the location is unset and a nearby Azure region is known. Null when the host has no
  /// <c>.aws.neon.tech</c> region, or the location is one of the nearby Azure regions. The text never
  /// contains the connection string or the endpoint id.
  /// </summary>
  internal static string? RegionDistanceWarning(string? connectionString, string? azureLocation)
  {
    string? awsRegion = AwsRegion(connectionString);
    if (awsRegion is null)
    {
      return null;
    }

    string? nearby = NearbyAzureRegion(awsRegion);
    string location = NormalizeLocation(azureLocation);
    if (nearby is null)
    {
      return string.IsNullOrEmpty(location)
        ? $"aca-neon: Neon is in AWS region {awsRegion}. No paired Azure region is listed. Set Azure:Location near that region and your users. Warning only; deploy continues."
        : $"aca-neon: Neon is in AWS region {awsRegion} and Azure:Location is {location.Trim()}. No paired Azure region is listed. Confirm they are near each other and near your users. Warning only; deploy continues.";
    }

    if (!string.IsNullOrEmpty(location) && IsNearbyLocation(awsRegion, location))
    {
      return null;
    }

    return string.IsNullOrEmpty(location)
      ? $"aca-neon: Neon is in AWS region {awsRegion}. Azure:Location is not set. Use {nearby} (near that region and your users) when Aspire asks, or set Azure:Location / Azure__Location first. Warning only; deploy continues."
      : $"aca-neon: Neon is in AWS region {awsRegion}, far from Azure location {azureLocation!.Trim()}. Use {nearby} instead (set Azure:Location or Azure__Location). Warning only; deploy continues.";
  }

  /// <summary>The AWS region label in a Neon host (<c>….&lt;region&gt;.aws.neon.tech</c>), or null.</summary>
  internal static string? AwsRegion(string? connectionString)
  {
    if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("://", StringComparison.Ordinal))
    {
      return null;
    }

    string? host = ReadHost(Parse(connectionString));
    if (host is null)
    {
      return null;
    }

    string[] labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    for (int index = 0; index < labels.Length - 3; index++)
    {
      if (labels[index + 1].Equals("aws", StringComparison.OrdinalIgnoreCase)
        && labels[index + 2].Equals("neon", StringComparison.OrdinalIgnoreCase)
        && labels[index + 3].Equals("tech", StringComparison.OrdinalIgnoreCase)
        && labels[index].Length > 0)
      {
        return labels[index].ToLowerInvariant();
      }
    }

    return null;
  }

  /// <summary>The Azure region to prefer for an AWS region Neon uses, or null when none is paired.</summary>
  internal static string? NearbyAzureRegion(string awsRegion) =>
    NearbyAzureRegions.TryGetValue(awsRegion.Trim().ToLowerInvariant(), out string[]? azureRegions) && azureRegions.Length > 0
      ? azureRegions[0]
      : null;

  private static bool IsNearbyLocation(string awsRegion, string normalizedLocation)
  {
    if (!NearbyAzureRegions.TryGetValue(awsRegion.Trim().ToLowerInvariant(), out string[]? azureRegions))
    {
      return false;
    }

    foreach (string azureRegion in azureRegions)
    {
      if (NormalizeLocation(azureRegion).Equals(normalizedLocation, StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static string NormalizeLocation(string? azureLocation)
  {
    if (string.IsNullOrWhiteSpace(azureLocation))
    {
      return "";
    }

    Span<char> buffer = stackalloc char[azureLocation.Length];
    int length = 0;
    foreach (char character in azureLocation)
    {
      if (character is ' ' or '_' or '-')
      {
        continue;
      }

      buffer[length++] = char.ToLowerInvariant(character);
    }

    return new string(buffer[..length]);
  }

  /// <summary>
  /// Nearby Azure regions for each AWS region Neon commonly places a project in.
  /// The first entry is the region a warning names.
  /// </summary>
  private static readonly Dictionary<string, string[]> NearbyAzureRegions = new(StringComparer.Ordinal)
  {
    ["us-east-1"] = ["eastus", "eastus2"],
    ["us-east-2"] = ["eastus2", "eastus", "centralus"],
    ["us-west-1"] = ["westus", "westus2"],
    ["us-west-2"] = ["westus2", "westus3", "westus"],
    ["eu-west-1"] = ["northeurope", "westeurope"],
    ["eu-west-2"] = ["uksouth", "ukwest"],
    ["eu-west-3"] = ["francecentral", "westeurope"],
    ["eu-central-1"] = ["germanywestcentral", "germanynorth", "westeurope"],
    ["eu-central-2"] = ["switzerlandnorth", "switzerlandwest"],
    ["eu-north-1"] = ["swedencentral"],
    ["eu-south-1"] = ["italynorth"],
    ["ap-southeast-1"] = ["southeastasia"],
    ["ap-southeast-2"] = ["australiaeast", "australiasoutheast"],
    ["ap-northeast-1"] = ["japaneast", "japanwest"],
    ["ap-northeast-2"] = ["koreacentral", "koreasouth"],
    ["ap-northeast-3"] = ["japaneast", "japanwest"],
    ["ap-south-1"] = ["centralindia", "southindia", "westindia"],
    ["ap-east-1"] = ["eastasia"],
    ["sa-east-1"] = ["brazilsouth", "brazilsoutheast"],
    ["ca-central-1"] = ["canadacentral", "canadaeast"],
    ["af-south-1"] = ["southafricanorth", "southafricawest"],
    ["me-south-1"] = ["uaenorth", "uaecentral"],
    ["il-central-1"] = ["israelcentral"],
  };

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
