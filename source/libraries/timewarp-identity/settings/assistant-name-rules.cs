#region Purpose
// The rules for the admin-set assistant name on SiteSettings: default Akili, trimmed, 1-32 plain-text characters.
#endregion

#region Design
// Task 361: one name labels every Ask entry point, the panel header, the greeting, the answer-bar
// feedback titles, the system prompt and the Multiavatar seed. The aggregate and the contract
// validator (SiteSettingsDetailsValidator) both call IsValid, so the two cannot disagree.
// Plain text means no control characters and no angle brackets: the name is rendered as text
// (Blazor encodes it), but it also goes into the model's instructions and feedback titles, where
// markup would be noise or an injection vector. Akili is Swahili for intelligence, mind or wit.
#endregion

namespace TimeWarp.Identity;

/// <summary>Default, length, and plain-text rules for <see cref="SiteSettings.AssistantName"/>.</summary>
public static class AssistantNameRules
{
  /// <summary>The template default name (Swahili for intelligence, mind or wit).</summary>
  public const string Default = "Akili";

  /// <summary>Maximum length after trimming.</summary>
  public const int MaxLength = 32;

  /// <summary>True when <paramref name="name"/>, trimmed, is 1-32 characters with no control characters or markup.</summary>
  public static bool IsValid(string? name)
  {
    if (name is null)
    {
      return false;
    }

    string trimmed = name.Trim();
    return trimmed.Length is > 0 and <= MaxLength && IsPlainText(trimmed);
  }

  /// <summary>True when <paramref name="name"/> has no control characters and no angle brackets.</summary>
  public static bool IsPlainText(string name)
  {
    ArgumentNullException.ThrowIfNull(name);
    foreach (char character in name)
    {
      if (char.IsControl(character) || character is '<' or '>')
      {
        return false;
      }
    }

    return true;
  }
}
