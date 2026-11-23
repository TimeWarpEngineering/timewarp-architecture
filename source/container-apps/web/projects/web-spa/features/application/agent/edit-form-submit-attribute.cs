#region Purpose
// Marks a page-scoped submit action whose page edits a command, so a prefill tool can draft it.
#endregion

#region Design
// The opt-in is this attribute, not a razor scan and not a source generator. CommandType is the
// working-copy type the page binds. SuggestionActionType is the catalog action that stores the
// draft. Tool generation reads both by reflection and never names a product slice.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>The submit action for one page's edit form, and the action that stores an agent draft of it.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EditFormSubmitAttribute : Attribute
{
  public EditFormSubmitAttribute(Type commandType, Type suggestionActionType)
  {
    ArgumentNullException.ThrowIfNull(commandType);
    ArgumentNullException.ThrowIfNull(suggestionActionType);
    CommandType = commandType;
    SuggestionActionType = suggestionActionType;
  }

  public Type CommandType { get; }

  public Type SuggestionActionType { get; }
}
