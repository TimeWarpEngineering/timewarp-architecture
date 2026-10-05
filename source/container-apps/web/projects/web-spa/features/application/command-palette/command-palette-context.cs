#region Purpose
// The current page's contextual Ctrl-K rows, gathered from every ICommandPaletteContextSource for the current path.
#endregion

#region Design
// Scoped service (one per circuit/tab, like the store). Rows() is what CommandPaletteState.Open adds
// to the roster; IsOffered() is the runner's fail-closed gate — a contextual row runs only if the
// current page still contributes exactly that row (target, arguments and follow-up included), so a
// row from an older payload, another page, or a hand-built row is refused. The path is the
// base-relative path without query or fragment, with a leading "/".
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public sealed class CommandPaletteContext
{
  private readonly IEnumerable<ICommandPaletteContextSource> Sources;
  private readonly NavigationManager NavigationManager;

  public CommandPaletteContext(IEnumerable<ICommandPaletteContextSource> sources, NavigationManager navigationManager)
  {
    Sources = sources;
    NavigationManager = navigationManager;
  }

  /// <summary>The current base-relative path, e.g. "/HypermediaLab".</summary>
  public string CurrentPath
  {
    get
    {
      string relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
      int cut = relative.IndexOfAny(['?', '#']);
      return "/" + (cut < 0 ? relative : relative[..cut]);
    }
  }

  /// <summary>Every contextual row the current page contributes now.</summary>
  public IReadOnlyList<CommandPaletteRow> Rows()
  {
    string path = CurrentPath;
    return [.. Sources.SelectMany(source => source.GetRows(path))];
  }

  /// <summary>True when the current page still contributes exactly <paramref name="row"/>.</summary>
  public bool IsOffered(CommandPaletteRow row) =>
    row.Kind == CommandPaletteRowKind.Contextual && Rows().Contains(row);
}
