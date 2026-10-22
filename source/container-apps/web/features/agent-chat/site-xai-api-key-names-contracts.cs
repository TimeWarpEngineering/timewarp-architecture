#region Purpose
// Name, Data Protection purpose, and accepted length of the site xAI API key.
#endregion

#region Design
// The purpose is specific to this secret. dotnet new rewrites the TimeWarp.Architecture literal
// with the generated app's name, same as the Data Protection application name, so two apps do not
// share a purpose string. Task 399 uses a different name and purpose on the same store. The
// minimum length keeps the stored suffix (four characters) from being the whole secret. The
// validator and the write path both call IsAcceptable so a suffix cannot leak the secret.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Identity and bounds of the site xAI API key.</summary>
public static class SiteXaiApiKeyNames
{
  /// <summary>Row name. One site key.</summary>
  public const string Name = "site.xai.api-key";

  /// <summary>Data Protection purpose. Not reused for any other secret.</summary>
  public const string Purpose = "TimeWarp.Architecture.XaiApiKey";

  /// <summary>Shortest accepted key. Longer than the displayed suffix.</summary>
  public const int MinimumLength = 8;

  /// <summary>Longest accepted key.</summary>
  public const int MaximumLength = 512;

  /// <summary>True when <paramref name="apiKey"/> can be stored without the suffix becoming the secret.</summary>
  public static bool IsAcceptable(string? apiKey)
  {
    if (string.IsNullOrWhiteSpace(apiKey))
    {
      return false;
    }

    string trimmed = apiKey.Trim();
    return trimmed.Length is >= MinimumLength and <= MaximumLength
      && !trimmed.Any(char.IsWhiteSpace);
  }
}
