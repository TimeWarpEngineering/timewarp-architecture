#region Purpose
// State holding agent drafts for edit forms, one per submit action, until the form applies them.
#endregion

#region Design
// Task 508 replaced the per-slice suggestion trio (Profile's Set, Confirm, Clear) with this one
// store. The key is the submit action's full type name (KeyFor), so any page-scoped edit form
// shares it and no product slice is named here. A draft is immutable JSON, a new id, and
// whether the person accepted it over unsaved edits. FormPrefillScope reads the draft for its
// submit action, applies it onto the form's working copy, and clears it. Initialize drops every
// draft; sign-out calls it so the next person never sees one.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>An agent draft of one edit form. It is not saved until the person submits.</summary>
public sealed record FormSuggestion(string Json, string Id, bool Accepted);

[StateAccess]
public sealed partial class FormSuggestionState : State<FormSuggestionState>
{
  private Dictionary<string, FormSuggestion> Drafts = new(StringComparer.Ordinal);

  /// <summary>Drafts by form key. Empty when no form has one waiting.</summary>
  public IReadOnlyDictionary<string, FormSuggestion> Suggestions => Drafts;

  /// <summary>The key for a submit action type: its full name.</summary>
  public static string KeyFor(Type submitActionType)
  {
    ArgumentNullException.ThrowIfNull(submitActionType);
    return submitActionType.FullName ?? submitActionType.Name;
  }

  /// <summary>The waiting draft for <paramref name="form"/>, or null.</summary>
  public FormSuggestion? For(string form)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(form);
    return Drafts.TryGetValue(form, out FormSuggestion? suggestion) ? suggestion : null;
  }

  public override void Initialize() => Drafts = new(StringComparer.Ordinal);

  private void Put(string form, FormSuggestion suggestion) =>
    Drafts = new(Drafts, StringComparer.Ordinal) { [form] = suggestion };

  private void Remove(string form)
  {
    if (!Drafts.ContainsKey(form))
    {
      return;
    }

    Dictionary<string, FormSuggestion> next = new(Drafts, StringComparer.Ordinal);
    next.Remove(form);
    Drafts = next;
  }
}
