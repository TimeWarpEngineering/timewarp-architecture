#region Purpose
// Turns selected catalog tools into AIFunction instances the chat client can invoke.
#endregion

#region Design
// Mutating tools are ApprovalRequiredAIFunction. That type records the requirement; it does not
// enforce it. FunctionInvokingChatClient turns the call into a tool-approval request and waits
// for a matching response before InvokeCoreAsync runs. The function reads IStore,
// AuthenticationStateProvider, IAuthorizationService, NavigationManager, and IActionCatalog from
// the arguments' service provider, which the invoking client sets from the circuit scope.
// Selection happened when the modal opened; by invocation the person may have navigated, signed
// out, or lost a permission while the approval waited. InvokeCoreAsync therefore re-selects for
// the current principal and route and returns a failed result (not an exception, so the model
// sees why) when the tool is no longer offered. It executes the re-selected entry.
// The model never calls an HTTP endpoint itself. Store handlers keep [EndpointAuthorize].
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Serialization;

/// <summary>Catalog tools as <see cref="AITool"/> instances, including approval wrappers.</summary>
public sealed class CatalogAgentFunctions : IDisposable
{
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
      CatalogAgentFunction function = new(tool, document);
      functions.Add(tool.RequiresApproval ? new ApprovalRequiredAIFunction(function) : function);
    }

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

    public CatalogAgentFunction(CatalogAgentTool tool, JsonDocument schema)
    {
      Tool = tool;
      Schema = schema;
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
        PageAgentScope.FromNavigation(navigation),
        Tool.Name,
        cancellationToken
      );
      if (current is null)
      {
        return new CatalogAgentCallResult(Tool.Name, Completed: false, "The action is not available on this page.");
      }

      object?[] bound = CatalogAgentArguments.Bind(current.Entry, arguments);
      outcome.Clear();
      await current.Entry.Execute(store, bound, cancellationToken);
      return new CatalogAgentCallResult(Tool.Name, Completed: true, Error: null, outcome.Result);
    }
  }

  private sealed record CatalogAgentCallResult(
    string Action,
    bool Completed,
    string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Result = null);
}
