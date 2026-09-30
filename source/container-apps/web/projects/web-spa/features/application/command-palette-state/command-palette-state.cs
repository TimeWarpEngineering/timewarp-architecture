#region Purpose
// Ctrl-K command palette state: the permitted roster, the query, the ranked matches, and the highlighted row.
#endregion

#region Design
// Visibility is not here: the overlay is a store-driven modal (ApplicationState.ActiveModalId ==
// CommandPalette.ModalId), so the palette shares the one-modal-at-a-time rule and the backdrop
// click-to-close with every other modal. This state only owns what the list shows.
// Open rebuilds Roster each time the palette opens, so a sign-in / permission change since the
// last open is honoured without a subscription. Filter re-ranks Roster (CommandPaletteRanker)
// and resets the highlight to the best match; with no matches nothing is highlighted, so Enter
// cannot run a near-miss. Running a row is not an action here: the component closes the modal
// and then navigates or executes (CommandPaletteRunner) — handlers do not nest dispatch.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class CommandPaletteState : State<CommandPaletteState>
{
  /// <summary>Every row the current principal may run (pages + commands), unranked.</summary>
  public IReadOnlyList<CommandPaletteRow> Roster { get; private set; } = [];

  public string Query { get; private set; } = "";

  /// <summary>Roster filtered and ordered for <see cref="Query"/>.</summary>
  public IReadOnlyList<CommandPaletteRow> Matches { get; private set; } = [];

  /// <summary>Index into <see cref="Matches"/>; -1 when there is nothing to run.</summary>
  public int HighlightedIndex { get; private set; } = -1;

  public CommandPaletteRow? Highlighted =>
    HighlightedIndex >= 0 && HighlightedIndex < Matches.Count ? Matches[HighlightedIndex] : null;

  public CommandPaletteState() { }

  public override void Initialize()
  {
    Roster = [];
    Query = "";
    Matches = [];
    HighlightedIndex = -1;
  }

  private void Apply(string query)
  {
    Query = query;
    Matches = CommandPaletteRanker.Rank(Roster, query);
    HighlightedIndex = Matches.Count > 0 ? 0 : -1;
  }
}
