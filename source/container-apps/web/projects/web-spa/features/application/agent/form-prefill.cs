#region Purpose
// Builds Prefill tools for edit-form submit actions and dispatches the draft storage action.
#endregion

#region Design
// The tool is synthetic, like navigate: it is not itself a catalog entry. The suggestion action
// (FormSuggestionState.SetFormSuggestion) is a catalog entry so Dispatch can Execute it.
// ISender.Send is TWA0022 in this project, so the pipeline does not call Send.
// Opt-in is derived, not declared (task 508). A catalog action is an edit-form submit when a
// page lists it in [Page(AgentActions = ...)] and its public Command property's type implements
// exactly one I*Details interface (tw-web-api-contracts: the edit form binds that interface).
// The [EditFormSubmit] attribute that 490 used is deleted: it repeated what the contract and the
// page already say. The draft schema and the accepted keys are that interface's properties, so
// RoleId, Version, and UserId are never drafted. A page renders FormPrefillScope<TSubmit> around
// the form to apply the draft; FormPrefillConvention_Should_ fails when a derived submit has no
// scope in this assembly. Product slices are not named here. Normalization and apply live on the
// other part of this type. Approval is Ask before editing on the page, and never for the
// off-page navigate offer. The form copies fields locally and then clears the suggestion.
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

  /// <summary>The draft storage action type when the catalog has it. It is never an agent tool.</summary>
  public static HashSet<Type> SuggestionTypes(IEnumerable<ActionCatalogEntry> entries)
  {
    ArgumentNullException.ThrowIfNull(entries);
    HashSet<Type> types = [];
    foreach (ActionCatalogEntry entry in entries)
    {
      if (entry.ActionType == typeof(FormSuggestionState.SetFormSuggestionActionSet.Action))
      {
        types.Add(entry.ActionType);
      }
    }

    return types;
  }

  /// <summary>
  /// The I*Details interface an edit form binds for <paramref name="actionType"/>, read from the
  /// action's public Command property. Null when there is no Command or not exactly one such interface.
  /// </summary>
  public static Type? DetailsType(Type actionType)
  {
    ArgumentNullException.ThrowIfNull(actionType);
    PropertyInfo? command = actionType.GetProperty("Command", BindingFlags.Instance | BindingFlags.Public);
    if (command?.GetMethod is null)
    {
      return null;
    }

    Type? found = null;
    foreach (Type candidate in command.PropertyType.GetInterfaces())
    {
      if (!candidate.Name.StartsWith('I', StringComparison.Ordinal) || !candidate.Name.EndsWith("Details", StringComparison.Ordinal))
      {
        continue;
      }

      if (found is not null)
      {
        return null;
      }

      found = candidate;
    }

    return found;
  }

  /// <summary>True when a page lists <paramref name="actionType"/> and its Command binds an I*Details interface.</summary>
  public static bool IsEditFormSubmit(Type actionType)
  {
    ArgumentNullException.ThrowIfNull(actionType);
    return DetailsType(actionType) is not null && PageAgentScope.IsPageBound(actionType);
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
    if (SuggestionTypes(entryList).Count == 0)
    {
      return [];
    }

    List<CatalogAgentTool> tools = [];
    foreach (ActionCatalogEntry entry in entryList)
    {
      if (!entry.Visibility.HasFlag(ActionVisibility.Agent) || !IsEditFormSubmit(entry.ActionType))
      {
        continue;
      }

      Type details = DetailsType(entry.ActionType)!;

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
        CatalogAgentSchema.OptionalCommand(details),
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

    if (!IsEditFormSubmit(submit.ActionType))
    {
      throw new InvalidOperationException($"'{submit.Name}' is not an edit-form submit.");
    }

    ActionCatalogEntry? suggestion = null;
    foreach (ActionCatalogEntry entry in catalog.Entries)
    {
      if (entry.ActionType == typeof(FormSuggestionState.SetFormSuggestionActionSet.Action))
      {
        suggestion = entry;
        break;
      }
    }

    if (suggestion is null)
    {
      throw new InvalidOperationException("No catalog entry executes FormSuggestionState.SetFormSuggestion.");
    }

    await suggestion.Execute(store, [FormSuggestionState.KeyFor(submit.ActionType), suggestionJson], cancellationToken);
    return new Result(Drafted: true, Error: null, fields);
  }

}
