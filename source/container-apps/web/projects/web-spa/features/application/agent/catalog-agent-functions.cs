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
// AgentNavigate.Offer and does not Execute. navigate is checked by name and calls
// AgentNavigate.InvokeAsync. A catalog tool executes the re-selected entry.
// The wrapper is chosen at Create from the edit mode then; each function remembers whether it was
// wrapped. If the re-selected tool now requires approval (the person switched back to Ask before
// editing mid-run) and this function was built unwrapped, it refuses with ApprovalRequiredError
// instead of running without a prompt. A wrapped function in Automatic mode still runs: the
// prompt it already showed is stricter than the mode.
// Create always appends page_context with the same name, description, and empty schema WebMCP
// publishes, and it is never approval-wrapped. The conversation credential is checked after the
// approval wrapper has already run, and before Execute. A null credential is allowed.
// The model never calls an HTTP endpoint itself. Store handlers keep [EndpointAuthorize].
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Serialization;

/// <summary>Catalog tools as <see cref="AITool"/> instances, including approval wrappers.</summary>
public sealed class CatalogAgentFunctions : IDisposable
{
  public const string ApprovalRequiredError =
    "This action now needs approval. The edit mode changed to Ask before editing; ask again.";

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

      // Re-read on every invocation. The function does not cache an AuthenticationState.
#pragma warning disable BL0013
      AuthenticationState authentication = await authenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
      CatalogAgentTool? current = await CatalogAgentToolSet.FindOfferedAsync
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
        return new CatalogAgentCallResult(Tool.Name, Completed: false, "The action is not available on this page.");
      }

      if (string.Equals(current.Name, AgentNavigate.ToolName, StringComparison.Ordinal))
      {
        return await AgentNavigate.InvokeAsync
        (
          authentication.User,
          authorizationService,
          catalog.Entries,
          store,
          PageAgentRoute.Current(services, navigation),
          arguments,
          cancellationToken
        );
      }

      if (current.OffPageRoute is not null)
      {
        if (current.Entry is null)
        {
          return new CatalogAgentCallResult(Tool.Name, Completed: false, "The action is not available on this page.");
        }

        string? offerDenial = AgentConversationAuthority.Denial(
          store.GetState<AgentSurfaceState>().Conversation,
          authentication.User,
          current.Entry.Permissions);
        if (offerDenial is not null)
        {
          return new CatalogAgentCallResult(Tool.Name, Completed: false, offerDenial);
        }

        return AgentNavigate.OfferFor(current.Entry, current.OffPageRoute);
      }

      if (current.Entry is null)
      {
        return new CatalogAgentCallResult(Tool.Name, Completed: false, "The action is not available on this page.");
      }

      if (current.RequiresApproval && !Wrapped)
      {
        return new CatalogAgentCallResult(Tool.Name, Completed: false, ApprovalRequiredError);
      }

      object?[] bound = CatalogAgentArguments.Bind(current.Entry, arguments);
      string? denial = AgentConversationAuthority.Denial(
        store.GetState<AgentSurfaceState>().Conversation,
        authentication.User,
        current.Entry.Permissions);
      if (denial is not null)
      {
        return new CatalogAgentCallResult(Tool.Name, Completed: false, denial);
      }

      outcome.Clear();
      await current.Entry.Execute(store, bound, cancellationToken);
      return new CatalogAgentCallResult(Tool.Name, Completed: true, Error: null, outcome.Result);
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
      return PageAgentContext.Describe(store, path, names);
    }
  }

  private sealed record CatalogAgentCallResult(
    string Action,
    bool Completed,
    string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Result = null);
}
