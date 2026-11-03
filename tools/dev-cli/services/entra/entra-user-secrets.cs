#region Purpose
// Parse `dotnet user-secrets list` output and merge SignInOptions:Methods entries.
#endregion

#region Design
// Setup appends Microsoft365 at the next Methods index when the merged checked-in list and user
// secrets do not already contain it, and does not rewrite other entries. Disable removes every
// user-secret Methods entry whose value is Microsoft365 and does not write Passkey.
// Key and method-name constants live on EntraSetup.
#endregion

namespace DevCli.Services;

using System.Globalization;
using System.Text.Json;

internal static class EntraUserSecrets
{
  internal static Dictionary<string, string> ParseUserSecretsList(string stdout)
  {
    Dictionary<string, string> secrets = new(StringComparer.OrdinalIgnoreCase);
    if (string.IsNullOrWhiteSpace(stdout))
    {
      return secrets;
    }

    foreach (string line in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
    {
      int separator = line.IndexOf(" = ", StringComparison.Ordinal);
      if (separator <= 0)
      {
        continue;
      }

      string key = line[..separator].Trim();
      string value = line[(separator + 3)..];
      if (key.Length == 0)
      {
        continue;
      }

      secrets[key] = value;
    }

    return secrets;
  }

  internal static bool TryParseSignInMethodIndex(string key, out int index)
  {
    index = -1;
    if (!key.StartsWith(EntraSetup.SignInMethodsPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return int.TryParse(key[EntraSetup.SignInMethodsPrefix.Length..], out index) && index >= 0;
  }

  internal static IReadOnlyList<string> MergeSignInMethods(
    IReadOnlyList<string> checkedIn,
    IReadOnlyDictionary<string, string> secrets)
  {
    SortedDictionary<int, string> byIndex = [];
    for (int index = 0; index < checkedIn.Count; index++)
    {
      byIndex[index] = checkedIn[index];
    }

    foreach ((string key, string value) in secrets)
    {
      if (TryParseSignInMethodIndex(key, out int index))
      {
        byIndex[index] = value;
      }
    }

    return [.. byIndex.Values];
  }

  internal static string? Microsoft365MethodKeyToAdd(
    IReadOnlyList<string> checkedIn,
    IReadOnlyDictionary<string, string> secrets)
  {
    if (MergeSignInMethods(checkedIn, secrets).Contains(EntraSetup.Microsoft365MethodName, StringComparer.Ordinal))
    {
      return null;
    }

    int next = checkedIn.Count;
    foreach (string key in secrets.Keys)
    {
      if (TryParseSignInMethodIndex(key, out int index))
      {
        next = Math.Max(next, index + 1);
      }
    }

    return EntraSetup.SignInMethodsPrefix + next.ToString(CultureInfo.InvariantCulture);
  }

  internal static IReadOnlyList<string> Microsoft365MethodKeysToRemove(IReadOnlyDictionary<string, string> secrets)
  {
    List<string> keys = [];
    foreach ((string key, string value) in secrets)
    {
      if (TryParseSignInMethodIndex(key, out _) && string.Equals(value, EntraSetup.Microsoft365MethodName, StringComparison.Ordinal))
      {
        keys.Add(key);
      }
    }

    keys.Sort(StringComparer.Ordinal);
    return keys;
  }

  internal static IReadOnlyList<string> ReadCheckedInSignInMethods(string appsettingsPath)
  {
    if (!File.Exists(appsettingsPath))
    {
      return [];
    }

    using var document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
    if (!document.RootElement.TryGetProperty("SignInOptions", out JsonElement section)
      || !section.TryGetProperty("Methods", out JsonElement methods)
      || methods.ValueKind != JsonValueKind.Array)
    {
      return [];
    }

    List<string> values = [];
    foreach (JsonElement item in methods.EnumerateArray())
    {
      if (item.ValueKind == JsonValueKind.String && item.GetString() is string value)
      {
        values.Add(value);
      }
    }

    return values;
  }

  internal static bool HasClientSecret(IReadOnlyDictionary<string, string> secrets)
  {
    return secrets.TryGetValue(EntraSetup.ClientSecretKey, out string? value)
      && !string.IsNullOrWhiteSpace(value);
  }
}
