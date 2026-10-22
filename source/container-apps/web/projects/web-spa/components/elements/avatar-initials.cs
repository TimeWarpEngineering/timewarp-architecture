#region Purpose
// Turns a person's name into one or two initials for the Avatar circle.
#endregion

#region Design
// The first text element of the first word, and of the last word when there is another word.
// A single word yields one element. Null, empty, and whitespace yield an empty string.
// StringInfo walks text elements, so a supplementary-plane character stays one unit.
// Casing is invariant. An email-shaped name is an ordinary single word: the first element only.
#endregion

namespace TimeWarp.Architecture.Components;

using System.Globalization;

public static class AvatarInitials
{
  public static string From(string? name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return "";
    }

    string[] words = name.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 0)
    {
      return "";
    }

    string first = FirstTextElement(words[0]);
    if (words.Length == 1)
    {
      return first.ToUpperInvariant();
    }

    string last = FirstTextElement(words[^1]);
    return string.Concat(first, last).ToUpperInvariant();
  }

  private static string FirstTextElement(string word)
  {
    StringInfo info = new(word);
    return info.LengthInTextElements == 0 ? "" : info.SubstringByTextElements(0, 1);
  }
}
