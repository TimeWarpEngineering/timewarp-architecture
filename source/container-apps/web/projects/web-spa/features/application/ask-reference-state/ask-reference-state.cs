#region Purpose
// Ask @ reference picker state: permitted resource types, the chosen type's search, the highlight,
// and the references chosen in the current conversation.
#endregion

#region Design
// Task 345. Typing @ in the Ask composer opens the picker (Open): the type list is every
// generated ResourceReferenceRegistry type the user may list. Filter narrows the type list by
// prefix or name, or, once a type is chosen, searches that type's list query as the user, with a
// short debounce (SearchDelay). Every search bumps SearchGeneration and a result is applied only
// if its generation is still current, so a slow answer never overwrites a newer term.
// ChooseType runs the first search at once; Back returns to the type list; Choose records the
// item in Chosen and closes. Chosen is per conversation: it is kept with the ConversationGeneration
// it was chosen in, and a reference from an older generation is dropped on the next Choose and
// ignored by page_context. Rows are strings only (ResourceReferenceTypeRow, ResourceReferenceItem)
// so the state clones without delegates. Inserting the token at the caret is the component's
// presentational JS; handlers do not touch the DOM.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class AskReferenceState : State<AskReferenceState>
{
  public static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(200);

  public const int ChosenCap = 20;

  public bool IsOpen { get; private set; }

  /// <summary>Types the signed-in user may list, read when the picker opened.</summary>
  public IReadOnlyList<ResourceReferenceTypeRow> Types { get; private set; } = [];

  /// <summary>The type being searched, or null while the type list shows.</summary>
  public ResourceReferenceTypeRow? SelectedType { get; private set; }

  public string Term { get; private set; } = "";

  public IReadOnlyList<ResourceReferenceItem> Items { get; private set; } = [];

  public bool HasMore { get; private set; }

  public bool IsSearching { get; private set; }

  public string? Problem { get; private set; }

  /// <summary>Index into the visible rows; -1 when there is nothing to choose.</summary>
  public int HighlightedIndex { get; private set; } = -1;

  public int SearchGeneration { get; private set; }

  /// <summary>References chosen in conversation <see cref="ChosenGeneration"/>.</summary>
  public IReadOnlyList<ResourceReferenceItem> Chosen { get; private set; } = [];

  public int ChosenGeneration { get; private set; }

  /// <summary>Type rows matching <see cref="Term"/>; empty once a type is chosen.</summary>
  public IReadOnlyList<ResourceReferenceTypeRow> VisibleTypes =>
    SelectedType is not null ? [] : MatchTypes(Types, Term);

  public int RowCount => SelectedType is null ? VisibleTypes.Count : Items.Count;

  public ResourceReferenceTypeRow? HighlightedType =>
    SelectedType is null && HighlightedIndex >= 0 && HighlightedIndex < VisibleTypes.Count
      ? VisibleTypes[HighlightedIndex]
      : null;

  public ResourceReferenceItem? HighlightedItem =>
    SelectedType is not null && HighlightedIndex >= 0 && HighlightedIndex < Items.Count
      ? Items[HighlightedIndex]
      : null;

  /// <summary>Chosen references that belong to <paramref name="conversationGeneration"/>.</summary>
  public IReadOnlyList<ResourceReferenceItem> ChosenFor(int conversationGeneration) =>
    ChosenGeneration == conversationGeneration ? Chosen : [];

  public AskReferenceState() { }

  public override void Initialize()
  {
    IsOpen = false;
    Types = [];
    Chosen = [];
    ChosenGeneration = 0;
    SearchGeneration = 0;
    ResetPicker();
  }

  public static IReadOnlyList<ResourceReferenceTypeRow> MatchTypes(IReadOnlyList<ResourceReferenceTypeRow> types, string term)
  {
    string text = term.Trim().TrimEnd(':');
    if (text.Length == 0)
    {
      return types;
    }

    List<ResourceReferenceTypeRow> matches = [];
    foreach (ResourceReferenceTypeRow type in types)
    {
      if (type.Prefix.Contains(text, StringComparison.OrdinalIgnoreCase)
        || type.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
      {
        matches.Add(type);
      }
    }

    return matches;
  }

  private void ResetPicker()
  {
    SelectedType = null;
    Term = "";
    Items = [];
    HasMore = false;
    IsSearching = false;
    Problem = null;
    HighlightedIndex = -1;
  }

  private void ShowTypes(string term)
  {
    SelectedType = null;
    Term = term;
    Items = [];
    HasMore = false;
    IsSearching = false;
    Problem = null;
    HighlightedIndex = VisibleTypes.Count > 0 ? 0 : -1;
  }

  /// <summary>Starts a search for the selected type; returns its generation.</summary>
  private int BeginSearch(string term)
  {
    Term = term;
    IsSearching = true;
    Problem = null;
    return ++SearchGeneration;
  }

  private void ApplySearch(ResourceReferenceSearch search)
  {
    Items = search.Items;
    HasMore = search.HasMore;
    Problem = search.Problem;
    IsSearching = false;
    HighlightedIndex = Items.Count > 0 ? 0 : -1;
  }

  /// <summary>Runs the selected type's search; applies it only while <paramref name="generation"/> is current.</summary>
  private static async Task SearchAsync
  (
    Func<AskReferenceState> state,
    IApiService apiService,
    int generation,
    TimeSpan delay,
    CancellationToken cancellationToken
  )
  {
    if (delay > TimeSpan.Zero)
    {
      await Task.Delay(delay, cancellationToken);
      if (state().SearchGeneration != generation)
      {
        return;
      }
    }

    AskReferenceState current = state();
    ResourceReferenceType? type = ResourceReferenceRegistry.Find(current.SelectedType?.Prefix);
    if (type is null)
    {
      return;
    }

    ResourceReferenceSearch search = await type.SearchAsync(apiService, current.Term, cancellationToken);
    AskReferenceState after = state();
    if (after.SearchGeneration == generation && after.SelectedType?.Prefix == type.Prefix)
    {
      after.ApplySearch(search);
    }
  }
}
