#region Purpose
// Builds Prefill tools for edit-form submit actions and dispatches the suggestion action.
#endregion

#region Design
// The tool is synthetic, like navigate: it is not itself a catalog entry. The suggestion action
// is a catalog entry so Dispatch can Execute it. ISender.Send is TWA0022 in this project, so the
// pipeline does not call Send. Product slices are not named; the EditFormSubmit attribute and the
// catalog carry the types. Normalization and apply live on the other part of this type. Approval
// is Ask before editing on the page, and never for the off-page navigate offer. The page copies
// fields locally and then clears the suggestion.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Serialization;

/// <summary>Prefill tools and the draft they store for a page's edit form.</summary>
public static partial class FormPrefill
{
  public const string Prefix = "Prefill.";

  /// <summary>Draft result. <see cref="Drafted"/> is false when the arguments are not a draft.</summary>
  public sealed record Result(
    [property: JsonPropertyName("drafted")] bool Drafted,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("fields")] IReadOnlyList<string> Fields);

  public static string ToolName(string actionName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
    return Prefix + actionName;
  }

  public static HashSet<Type> SuggestionTypes(IEnumerable<ActionCatalogEntry> entries)
  {
    ArgumentNullException.ThrowIfNull(entries);
    HashSet<Type> types = [];
    foreach (ActionCatalogEntry entry in entries)
    {
      EditFormSubmitAttribute? attribute = entry.ActionType.GetCustomAttribute<EditFormSubmitAttribute>();
      if (attribute is not null)
      {
        types.Add(attribute.SuggestionActionType);
      }
    }

    return types;
  }

  public static async Task<IReadOnlyList<CatalogAgentTool>> SelectAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<ActionCatalogEntry> entries,
    string? path,
    AgentEditMode editMode,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(entries);
    cancellationToken.ThrowIfCancellationRequested();

    if (user.Identity?.IsAuthenticated != true)
    {
      return [];
    }

    List<ActionCatalogEntry> entryList = [.. entries];
    HashSet<Type> catalogTypes = [];
    foreach (ActionCatalogEntry entry in entryList)
    {
      catalogTypes.Add(entry.ActionType);
    }

    List<CatalogAgentTool> tools = [];
    foreach (ActionCatalogEntry entry in entryList)
    {
      EditFormSubmitAttribute? attribute = entry.ActionType.GetCustomAttribute<EditFormSubmitAttribute>();
      if (attribute is null || !entry.Visibility.HasFlag(ActionVisibility.Agent))
      {
        continue;
      }

      if (!catalogTypes.Contains(attribute.SuggestionActionType))
      {
        continue;
      }

      if (!PageAgentScope.IsPageBound(entry.ActionType))
      {
        continue;
      }

      if (!await CommandPaletteRoster.IsPermittedAsync(user, authorizationService, entry))
      {
        continue;
      }

      bool onPage = PageAgentScope.Serves(path, entry.ActionType);
      string? offPage = null;
      if (!onPage)
      {
        offPage = PageAgentScope.PrimaryPage(entry.ActionType);
        if (offPage is null)
        {
          continue;
        }
      }

      string description =
        "Draft values for " + entry.Name
        + ". Does not save. Every field is optional; only fields you pass are written onto the form for the person to review.";
      if (offPage is not null)
      {
        description += " It runs only on " + offPage
          + ". From any other page it does not run and returns a navigate offer.";
      }
      else if (editMode == AgentEditMode.AskBeforeEditing)
      {
        description += " The person approves the draft first.";
      }
      else
      {
        description += " It runs without a prompt in this conversation.";
      }

      tools.Add(new CatalogAgentTool
      (
        ToolName(entry.Name),
        description,
        CatalogAgentSchema.OptionalCommand(attribute.CommandType),
        RequiresApproval: onPage && editMode == AgentEditMode.AskBeforeEditing,
        entry,
        offPage,
        Prefill: true
      ));
    }

    tools.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));
    return tools;
  }

  public static async Task<Result> DispatchAsync
  (
    IActionCatalog catalog,
    IStore store,
    ActionCatalogEntry submit,
    string suggestionJson,
    IReadOnlyList<string> fields,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(catalog);
    ArgumentNullException.ThrowIfNull(store);
    ArgumentNullException.ThrowIfNull(submit);
    ArgumentNullException.ThrowIfNull(suggestionJson);
    ArgumentNullException.ThrowIfNull(fields);
    cancellationToken.ThrowIfCancellationRequested();

    EditFormSubmitAttribute attribute = submit.ActionType.GetCustomAttribute<EditFormSubmitAttribute>()
      ?? throw new InvalidOperationException($"'{submit.Name}' is not an edit-form submit.");

    ActionCatalogEntry? suggestion = null;
    foreach (ActionCatalogEntry entry in catalog.Entries)
    {
      if (entry.ActionType == attribute.SuggestionActionType)
      {
        suggestion = entry;
        break;
      }
    }

    if (suggestion is null)
    {
      throw new InvalidOperationException(
        "No catalog entry executes " + attribute.SuggestionActionType.FullName + ".");
    }

    await suggestion.Execute(store, [suggestionJson], cancellationToken);
    return new Result(Drafted: true, Error: null, fields);
  }

}
