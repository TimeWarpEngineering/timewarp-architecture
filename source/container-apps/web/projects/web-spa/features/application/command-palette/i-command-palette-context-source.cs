#region Purpose
// Port a page implements to contribute contextual Ctrl-K rows while it is the current page.
#endregion

#region Design
// Pull, not push (task 275): the palette asks every registered source for the CURRENT path each time
// it opens and again before it runs a contextual row. A source answers from its own store state, so
// the rows always reflect the latest server payload, and leaving the page "clears" them by
// construction — a source returns nothing for a path that is not its page. No SetContextualRows
// action, no clear-on-navigation listener, no stale rows after a refresh.
// The port lives in Applications (platform) and speaks CommandPaletteRow only, so a product slice
// implements it without Applications referencing the slice (TWA0009 stays one-way).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public interface ICommandPaletteContextSource
{
  /// <summary>Contextual rows for <paramref name="currentPath"/> (base-relative, query stripped); empty when it is not this source's page.</summary>
  IReadOnlyList<CommandPaletteRow> GetRows(string currentPath);
}
