#region Purpose
// Maps WebAuthn authenticator AAGUID bytes to human-readable passkey provider names
// (e.g. "Proton Pass", "1Password") for account Settings labels.
#endregion

#region Design
// How passkeys.io shows "Proton Pass" after create: the authenticator embeds a 16-byte AAGUID in
// attested credential data; RPs look it up in the community AAGUID list
// (passkeydeveloper/passkey-authenticator-aaguids). This is NOT a secret API from the password
// manager — just a public model id. Zero AAGUIDs (common with some platform paths) resolve to null.
// Names-only JSON is embedded as a resource (icons omitted — Settings uses text labels).
// Lookup is case-insensitive on the UUID string; Guid construction is avoided because WebAuthn
// AAGUIDs are big-endian UUID layout, not .NET mixed-endian Guid bytes.
#endregion

namespace TimeWarp.Identity;

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;

public static class PasskeyProviderNames
{
  private static readonly Lazy<FrozenDictionary<string, string>> Map = new(LoadMap);

  /// <summary>
  /// Resolves a 16-byte AAGUID to a provider display name, or null when unknown / all-zero.
  /// </summary>
  public static string? TryResolve(ReadOnlySpan<byte> aaguid)
  {
    if (aaguid.Length != 16 || IsAllZero(aaguid))
    {
      return null;
    }

    string key = FormatAaguid(aaguid);
    return Map.Value.TryGetValue(key, out string? name) ? name : null;
  }

  public static string? TryResolve(byte[]? aaguid) =>
    aaguid is null ? null : TryResolve(aaguid.AsSpan());

  private static bool IsAllZero(ReadOnlySpan<byte> aaguid)
  {
    for (int i = 0; i < aaguid.Length; i++)
    {
      if (aaguid[i] != 0)
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>Formats 16 big-endian AAGUID bytes as lowercase UUID with dashes.</summary>
  internal static string FormatAaguid(ReadOnlySpan<byte> aaguid)
  {
    // 8-4-4-4-12 hex
    return string.Create(36, aaguid, static (dest, src) =>
    {
      WriteHex(dest, 0, src[0]);
      WriteHex(dest, 2, src[1]);
      WriteHex(dest, 4, src[2]);
      WriteHex(dest, 6, src[3]);
      dest[8] = '-';
      WriteHex(dest, 9, src[4]);
      WriteHex(dest, 11, src[5]);
      dest[13] = '-';
      WriteHex(dest, 14, src[6]);
      WriteHex(dest, 16, src[7]);
      dest[18] = '-';
      WriteHex(dest, 19, src[8]);
      WriteHex(dest, 21, src[9]);
      dest[23] = '-';
      WriteHex(dest, 24, src[10]);
      WriteHex(dest, 26, src[11]);
      WriteHex(dest, 28, src[12]);
      WriteHex(dest, 30, src[13]);
      WriteHex(dest, 32, src[14]);
      WriteHex(dest, 34, src[15]);
    });
  }

  private static void WriteHex(Span<char> dest, int offset, byte value)
  {
    dest[offset] = ToHexNibble(value >> 4);
    dest[offset + 1] = ToHexNibble(value & 0xF);
  }

  private static char ToHexNibble(int nibble) =>
    (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));

  private static FrozenDictionary<string, string> LoadMap()
  {
    Assembly assembly = typeof(PasskeyProviderNames).Assembly;
    const string resourceName = "TimeWarp.Identity.ceremonies.webauthn.passkey-provider-aaguids.json";

    // Resource name may vary with root namespace; fall back to ends-with search.
    Stream? stream = assembly.GetManifestResourceStream(resourceName);
    if (stream is null)
    {
      string? match = assembly.GetManifestResourceNames()
        .FirstOrDefault(n => n.EndsWith("passkey-provider-aaguids.json", StringComparison.Ordinal));
      if (match is not null)
      {
        stream = assembly.GetManifestResourceStream(match);
      }
    }

    if (stream is null)
    {
      return FrozenDictionary<string, string>.Empty;
    }

    using (stream)
    {
      Dictionary<string, string>? dict =
        JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
      if (dict is null || dict.Count == 0)
      {
        return FrozenDictionary<string, string>.Empty;
      }

      // Normalize keys to lowercase for lookup.
      return dict.ToDictionary(
          static kv => kv.Key.ToLowerInvariant(),
          static kv => kv.Value,
          StringComparer.Ordinal)
        .ToFrozenDictionary(StringComparer.Ordinal);
    }
  }
}
