#region Purpose
// Turns selected catalog tools into AIFunction instances the chat client can invoke.
#endregion

#region Design
// Mutating tools are ApprovalRequiredAIFunction. That type records the requirement; it does not
// enforce it. FunctionInvokingChatClient turns the call into a tool-approval request and waits
// for a matching response before InvokeCoreAsync runs. The function reads IStore,
// AuthenticationStateProvider, IAuthorizationService, NavigationManager, and IActionCatalog from
// the arguments' service provider, which the invoking client sets from the circuit scope.
// The route is PageAgentRoute when the shell has observed one, so a NavigationManager left at
// the base URI does not describe the site root while the page on screen is /Feedback.
// Selection happened when the panel built the functions; by invocation the person may have navigated, signed
// out, or lost a permission while the approval waited. InvokeCoreAsync therefore re-selects for
// the current principal and route and returns a failed result (not an exception, so the model
// sees why) when the tool is no longer offered. A global palette tool is still offered after
// navigation. A page-only tool is not. A page-bound palette tool offered off its page returns
// AgentNavigate.Offer and does not Execute. site_map and navigate are checked by name and call
// AgentSiteMap.InvokeAsync and AgentNavigate.InvokeAsync; navigate is wrapped for approval in Ask
// before editing like any edit (task 300). A catalog tool executes the re-selected entry.
// A Prefill tool does not bind that entry: it validates the draft and executes the suggestion action.
// The wrapper is chosen at Create from the edit mode then; each function remembers whether it was
// wrapped. If the re-selected tool now requires approval (the person switched back to Ask before
// editing mid-run) and this function was built unwrapped, it refuses with ApprovalRequiredError
// instead of running without a prompt. A wrapped function in Automatic mode still runs: the
// prompt it already showed is stricter than the mode.
// A function built as an off-page offer (Wrapped false, OffPageRoute set) can be invoked again
// after this same turn navigates onto its page. Re-selection then has no OffPageRoute and, in
// Ask before editing, RequiresApproval. That is not an edit-mode change: ArrivalNeedsApprovalError
// tells the model to finish the turn without calling the tool or navigate again. The idle rebuild
// wraps the tool, so the next turn can ask. In Automatically edit the re-selected tool does not
// require approval, so this same unwrapped function runs the prefill.
// Create always appends page_context with the same name, description, and empty schema WebMCP
// publishes, and it is never approval-wrapped. page_context walks the page body when it is
// called, like the WebMCP dispatcher, so its text matches the page on screen. The conversation credential is checked after the
// approval wrapper has already run, and before Execute. A null credential is allowed.
// The model never calls an HTTP endpoint itself. Store handlers keep [EndpointAuthorize].
// An outcome that holds a picture (IAgentImageOutcome, task 303-001) is returned without it; the
// picture rides on the call result's Image, which RelayChatClient sends as image content.
// AskDiagnostics records a Development-only start and outcome for each InvokeCoreAsync return.
// A throw after the start writes tool-outcome with the exception type name and is rethrown.
// The line names the tool, route, and flags. It never includes draft JSON, destination lists, or
// the raw error text. The not-available sentence is WebMcpDispatcher.UnavailableError.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Serialization;

/// <summary>Catalog tools as <see cref="AITool"/> instances, including approval wrappers.</summary>
public sealed class CatalogAgentFunctions : IDisposable
{
  public const string ApprovalRequiredError =
    "This action now needs approval. The edit mode changed to Ask before editing; ask again.";

  public const string ArrivalNeedsApprovalError =
    "You are on the page now. This action needs approval, and approval is available on the next turn. Tell the person you arrived and stop. Do not call this tool or navigate again in this turn.";

  private readonly List<JsonDocument> Documents;

  private CatalogAgentFunctions(IReadOnlyList<AITool> tools, List<JsonDocument> documents)
  {
    Tools = tools;
    Documents = documents;
  }

  public IReadOnlyList<AITool> Tools { get; }

  public static CatalogAgentFunctions Create(IReadOnlyList<CatalogAgentTool> tools)
  {
    ArgumentNullException.ThrowIfNull(tools);
    List<AITool> functions = [];
    List<JsonDocument> documents = [];
    foreach (CatalogAgentTool tool in tools)
    {
      var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(tool.InputSchema) ? "{}" : tool.InputSchema);
      documents.Add(document);
      CatalogAgentFunction function = new(tool, document, wrapped: tool.RequiresApproval);
      functions.Add(tool.RequiresApproval ? new ApprovalRequiredAIFunction(function) : function);
    }

    var pageContextSchema = JsonDocument.Parse(PageAgentContext.EmptyInputSchema);
    documents.Add(pageContextSchema);
    functions.Add(new PageContextFunction(pageContextSchema));
    return new CatalogAgentFunctions(functions, documents);
  }

  public void Dispose()
  {
    foreach (JsonDocument document in Documents)
    {
      document.Dispose();
    }

    Documents.Clear();
  }

  private sealed class CatalogAgentFunction : AIFunction
  {
    private readonly CatalogAgentTool Tool;
    private readonly JsonDocument Schema;
    private readonly bool Wrapped;

    public CatalogAgentFunction(CatalogAgentTool tool, JsonDocument schema, bool wrapped)
    {
      Tool = tool;
      Schema = schema;
      Wrapped = wrapped;
    }

    public override string Name => Tool.Name;

    public override string Description => Tool.Description;

    public override JsonElement JsonSchema => Schema.RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync
    (
      AIFunctionArguments arguments,
      CancellationToken cancellationToken
    )
    {
      IServiceProvider services = arguments.Services
        ?? throw new InvalidOperationException($"Tool '{Tool.Name}' needs a service provider on the function arguments.");
      IStore store = services.GetRequiredService<IStore>();
      AuthenticationStateProvider authenticationStateProvider =
        services.GetRequiredService<AuthenticationStateProvider>();
      IAuthorizationService authorizationService = services.GetRequiredService<IAuthorizationService>();
      NavigationManager navigation = services.GetRequiredService<NavigationManager>();
      IActionCatalog catalog = services.GetRequiredService<IActionCatalog>();
      AgentCallOutcome outcome = services.GetRequiredService<AgentCallOutcome>();
      string route = PageAgentRoute.Current(services, navigation);
      AskDiagnostics diagnostics = services.GetService<AskDiagnostics>() ?? AskDiagnostics.Disabled;
      diagnostics.ToolStart(Tool.Name, route, Wrapped);
      CatalogAgentTool? current = null;
      try
      {

        // Re-read on every invocation. The function does not cache an AuthenticationState.
#pragma warning disable BL0013
        AuthenticationState authentication = await authenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
        current = await CatalogAgentToolSet.FindOfferedAsync
        (
          authentication.User,
          authorizationService,
          catalog.Entries,
          PageAgentRoute.Current(services, navigation),
          Tool.Name,
          store.GetState<AgentSurfaceState>().EditMode,
          cancellationToken
        );
        if (current is null)
        {
          return Trace(diagnostics, route, current: null, new CatalogAgentCallResult(Tool.Name, Completed: false, WebMcpDispatcher.UnavailableError));
        }

        if (string.Equals(current.Name, AgentSiteMap.ToolName, StringComparison.Ordinal))
        {
          return Trace(
            diagnostics,
            route,
            current,
            await AgentSiteMap.InvokeAsync
            (
              authentication.User,
              authorizationService,
              store,
              PageAgentRoute.Current(services, navigation),
              cancellationToken
            ));
        }

        if (string.Equals(current.Name, AgentNavigate.ToolName, StringComparison.Ordinal))
        {
          CatalogAgentCallResult? navigateApproval = RefuseIfApprovalMissing(current);
          if (navigateApproval is not null)
          {
            return Trace(diagnostics, route, current, navigateApproval);
          }

          return Trace(
            diagnostics,
            route,
            current,
            await AgentNavigate.InvokeAsync
            (
              authentication.User,
              authorizationService,
              store,
              PageAgentRoute.Current(services, navigation),
              arguments,
              cancellationToken
            ));
        }

        if (current.OffPageRoute is not null)
        {
          if (current.Entry is null)
          {
            return Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: false, WebMcpDispatcher.UnavailableError));
          }

          string? offerDenial = AgentConversationAuthority.Denial(
            store.GetState<AgentSurfaceState>().Conversation,
            authentication.User,
            current.Entry.Permissions);
          if (offerDenial is not null)
          {
            return Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: false, offerDenial));
          }

          return Trace(diagnostics, route, current, AgentNavigate.OfferFor(current.Entry, current.OffPageRoute));
        }

        if (current.Entry is null)
        {
          return Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: false, WebMcpDispatcher.UnavailableError));
        }

        if (current.Prefill)
        {
          CatalogAgentCallResult? prefillApproval = RefuseIfApprovalMissing(current);
          if (prefillApproval is not null)
          {
            return Trace(diagnostics, route, current, prefillApproval);
          }

          string? prefillDenial = AgentConversationAuthority.Denial(
            store.GetState<AgentSurfaceState>().Conversation,
            authentication.User,
            current.Entry.Permissions);
          if (prefillDenial is not null)
          {
            return Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: false, prefillDenial));
          }

          Type? details = FormPrefill.DetailsType(current.Entry.ActionType);
          if (details is null)
          {
            return Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: false, WebMcpDispatcher.UnavailableError));
          }

          if (!FormPrefill.TryNormalize(details, arguments, out string suggestionJson, out string? error, out IReadOnlyList<string> fields))
          {
            return Trace(diagnostics, route, current, new FormPrefill.Result(Drafted: false, error, fields));
          }

          return Trace(
            diagnostics,
            route,
            current,
            await FormPrefill.DispatchAsync(
              catalog,
              store,
              current.Entry,
              suggestionJson,
              fields,
              cancellationToken));
        }

        CatalogAgentCallResult? approval = RefuseIfApprovalMissing(current);
        if (approval is not null)
        {
          return Trace(diagnostics, route, current, approval);
        }

        object?[] bound = CatalogAgentArguments.Bind(current.Entry, arguments);
        string? denial = AgentConversationAuthority.Denial(
          store.GetState<AgentSurfaceState>().Conversation,
          authentication.User,
          current.Entry.Permissions);
        if (denial is not null)
        {
          return Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: false, denial));
        }

        outcome.Clear();
        await current.Entry.Execute(store, bound, cancellationToken);
        return outcome.Result is IAgentImageOutcome image
          ? Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: true, Error: null, image.WithoutImage(), image.ToImage()))
          : Trace(diagnostics, route, current, new CatalogAgentCallResult(Tool.Name, Completed: true, Error: null, outcome.Result));
      }
      catch (Exception exception)
      {
        diagnostics.ToolOutcome(
          Tool.Name,
          route,
          Wrapped,
          currentFound: current is not null,
          requiresApproval: current?.RequiresApproval ?? false,
          offPageRoute: current?.OffPageRoute,
          prefill: current?.Prefill ?? false,
          completed: false,
          error: exception.GetType().Name,
          fieldNames: null);
        throw;
      }
    }

    private object? Trace(AskDiagnostics diagnostics, string route, CatalogAgentTool? current, object? result)
    {
      bool completed;
      string? error;
      IReadOnlyList<string>? fields = null;
      string? offPageRoute = current?.OffPageRoute;
      bool prefill = current?.Prefill ?? false;
      switch (result)
      {
        case CatalogAgentCallResult call:
          completed = call.Completed;
          error = call.Error;
          break;
        case FormPrefill.Result draft:
          completed = draft.Drafted;
          error = draft.Error;
          fields = draft.Fields;
          prefill = true;
          break;
        case AgentNavigate.Offer offer:
          completed = offer.Executed;
          error = null;
          offPageRoute = offer.NavigateTo;
          break;
        case AgentNavigate.Result navigated:
          completed = navigated.Navigated;
          error = navigated.Error;
          break;
        case AgentSiteMap.Result map:
          completed = map.Error is null;
          error = map.Error;
          break;
        default:
          completed = false;
          error = "Failed";
          break;
      }

      diagnostics.ToolOutcome(
        Tool.Name,
        route,
        Wrapped,
        currentFound: current is not null,
        requiresApproval: current?.RequiresApproval ?? false,
        offPageRoute,
        prefill,
        completed,
        error,
        fields);
      return result;
    }

    private CatalogAgentCallResult? RefuseIfApprovalMissing(CatalogAgentTool current)
    {
      if (!current.RequiresApproval || Wrapped)
      {
        return null;
      }

      string error = Tool.OffPageRoute is not null && current.OffPageRoute is null
        ? ArrivalNeedsApprovalError
        : ApprovalRequiredError;
      return new CatalogAgentCallResult(Tool.Name, Completed: false, error);
    }
  }

  private sealed class PageContextFunction : AIFunction
  {
    private readonly JsonDocument Schema;

    public PageContextFunction(JsonDocument schema)
    {
      Schema = schema;
    }

    public override string Name => PageAgentContext.ToolName;

    public override string Description => PageAgentContext.ToolDescription;

    public override JsonElement JsonSchema => Schema.RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync
    (
      AIFunctionArguments arguments,
      CancellationToken cancellationToken
    )
    {
      IServiceProvider services = arguments.Services
        ?? throw new InvalidOperationException("Tool 'page_context' needs a service provider on the function arguments.");
      IStore store = services.GetRequiredService<IStore>();
      AuthenticationStateProvider authenticationStateProvider =
        services.GetRequiredService<AuthenticationStateProvider>();
      IAuthorizationService authorizationService = services.GetRequiredService<IAuthorizationService>();
      NavigationManager navigation = services.GetRequiredService<NavigationManager>();
      IActionCatalog catalog = services.GetRequiredService<IActionCatalog>();
#pragma warning disable BL0013
      AuthenticationState authentication = await authenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
      string? denial = AgentConversationAuthority.Denial(
        store.GetState<AgentSurfaceState>().Conversation,
        authentication.User,
        []);
      if (denial is not null)
      {
        return new CatalogAgentCallResult(PageAgentContext.ToolName, Completed: false, denial);
      }

      string path = PageAgentRoute.Current(services, navigation);
      IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
      (
        authentication.User,
        authorizationService,
        catalog.Entries,
        path,
        store.GetState<AgentSurfaceState>().EditMode,
        cancellationToken
      );
      List<string> names = [];
      foreach (CatalogAgentTool tool in selected)
      {
        names.Add(tool.Name);
      }

      names.Add(PageAgentContext.ToolName);
      string? surface = await PageSurfaceJsModule.TrySummarizeAsync(services.GetService<IJSRuntime>(), cancellationToken);
      return PageAgentContext.Describe(store, path, names, surface, PageAgentContext.DocumentCap);
    }
  }

  private sealed record CatalogAgentCallResult(
    string Action,
    bool Completed,
    string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Result = null,
    [property: JsonIgnore] DataContent? Image = null) : IToolResultImage;
}
