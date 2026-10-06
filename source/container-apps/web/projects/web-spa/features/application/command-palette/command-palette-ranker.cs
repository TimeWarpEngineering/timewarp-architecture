#region Purpose
// Deterministic fuzzy filter for the Ctrl-K palette: ranks rows by how the query matches name + description.
#endregion

#region Design
// No Jev / LLM ranking (task 238 deferred it): plain ordinal-ignore-case matching so the same
// query always yields the same list. Tiers, best first: name prefix, name word-start, name
// substring, description prefix / word-start / substring, then name subsequence ("stg" →
// Settings) as the loose fuzzy tail. A row takes its best tier; rows matching nothing drop out.
// Ties break on shorter name, then name, then kind — total order, so tests can pin it.
// An empty query keeps every row (pages first, then commands, each by name) so the palette
// opens as a browsable list. The ranker never picks a row to run: Enter runs the highlighted
// row, and a query with no match leaves nothing highlighted.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public static class CommandPaletteRanker
{
  private const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

  public static IReadOnlyList<CommandPaletteRow> Rank(IEnumerable<CommandPaletteRow> rows, string? query)
  {
    string trimmed = query?.Trim() ?? "";
    if (trimmed.Length == 0)
    {
      return
      [
        .. rows
          .OrderBy(static row => row.Kind)
          .ThenBy(static row => row.Name, StringComparer.OrdinalIgnoreCase)
          .ThenBy(static row => row.Name, StringComparer.Ordinal)
      ];
    }

    return
    [
      .. rows
        .Select(row => (Row: row, Score: Score(row, trimmed)))
        .Where(static scored => scored.Score > 0)
        .OrderByDescending(static scored => scored.Score)
        .ThenBy(static scored => scored.Row.Name.Length)
        .ThenBy(static scored => scored.Row.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(static scored => scored.Row.Kind)
        .Select(static scored => scored.Row)
    ];
  }

  /// <summary>Match tier for one row: 7 (name prefix) down to 1 (name subsequence); 0 = no match.</summary>
  public static int Score(CommandPaletteRow row, string query)
  {
    int name = TextTier(row.Name, query);
    if (name > 0)
    {
      return name + 3;
    }

    int description = TextTier(row.Description, query);
    if (description > 0)
    {
      return description;
    }

    return IsSubsequence(row.Name, query) ? 1 : 0;
  }

  // 4 = prefix, 3 = word start, 2 = substring; Score lifts name tiers above description tiers.
  private static int TextTier(string text, string query)
  {
    if (text.StartsWith(query, Comparison))
    {
      return 4;
    }

    int index = text.IndexOf(query, Comparison);
    if (index < 0)
    {
      return 0;
    }

    while (index >= 0)
    {
      if (IsWordStart(text, index))
      {
        return 3;
      }

      index = text.IndexOf(query, index + 1, Comparison);
    }

    return 2;
  }

  private static bool IsWordStart(string text, int index)
  {
    if (index == 0)
    {
      return true;
    }

    char previous = text[index - 1];
    char current = text[index];
    return !char.IsLetterOrDigit(previous) || (char.IsLower(previous) && char.IsUpper(current));
  }

  private static bool IsSubsequence(string text, string query)
  {
    int position = 0;
    foreach (char character in query)
    {
      if (char.IsWhiteSpace(character))
      {
        continue;
      }

      position = text.IndexOf(character.ToString(), position, Comparison);
      if (position < 0)
      {
        return false;
      }

      position++;
    }

    return true;
  }
}
