#region Purpose
// The rules for the admin-set assistant name on SiteSettings: default Akili, trimmed, 1-32 plain-text characters.
#endregion

#region Design
// Task 361: one name labels every Ask entry point, the panel header, the greeting, the
// answer-bar feedback titles, the system prompt and the Multiavatar seed. The aggregate and the
// contract validator (SiteSettingsDetailsValidator, after its NotEmpty required-message rule)
// both call IsValid, so the two cannot disagree.
// Plain text is checked per Unicode scalar (Rune), not per UTF-16 char, so astral characters
// are classified correctly. It means valid UTF-16 (no lone surrogates), no control characters
// (Cc), no format characters (Cf: bidi overrides and isolates, zero-width characters, the
// U+E0000 tag block), no line or paragraph separators (Zl, Zp) and no angle brackets. The name
// is rendered as text (Blazor encodes it), but it also goes into the model's instructions and
// feedback titles, where markup, a line break or invisible tag characters would be noise or an
// injection vector, and into labels, where bidi and zero-width characters allow spoofed or
// invisible names. Single emoji such as U+1F916 are allowed; ZWJ emoji sequences are not,
// because U+200D ZERO WIDTH JOINER is a format character and every format character is
// rejected. Lone surrogates are detected as invalid data from Rune.DecodeFromUtf16, so a
// literal U+FFFD typed by the admin is not mistaken for one.
// Akili is Swahili for intelligence, mind or wit.
#endregion

namespace TimeWarp.Identity;

using System.Buffers;
using System.Globalization;
using System.Text;

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

  /// <summary>
  /// True when <paramref name="name"/> is valid UTF-16 with no control, format, line-separator or
  /// paragraph-separator characters and no angle brackets.
  /// </summary>
  public static bool IsPlainText(string name)
  {
    ArgumentNullException.ThrowIfNull(name);
    ReadOnlySpan<char> remaining = name;
    while (!remaining.IsEmpty)
    {
      if (Rune.DecodeFromUtf16(remaining, out Rune rune, out int consumed) != OperationStatus.Done)
      {
        return false;
      }

      if (rune.Value is '<' or '>' || IsDisallowedCategory(Rune.GetUnicodeCategory(rune)))
      {
        return false;
      }

      remaining = remaining[consumed..];
    }

    return true;
  }

  private static bool IsDisallowedCategory(UnicodeCategory category) =>
    category is UnicodeCategory.Control
      or UnicodeCategory.Format
      or UnicodeCategory.LineSeparator
      or UnicodeCategory.ParagraphSeparator;
}
