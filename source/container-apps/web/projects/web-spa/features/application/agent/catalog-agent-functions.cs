#region Purpose
// Turns selected catalog tools into AIFunction instances the chat client can invoke.
#endregion

#region Design
// Mutating tools are ApprovalRequiredAIFunction. That type records the requirement; it does not
// enforce it. FunctionInvokingChatClient turns the call into a tool-approval request and waits
// for a matching response before InvokeCoreAsync runs. The function reads IStore from the
// arguments' service provider, which the invoking client sets from the circuit scope.
// The model never calls an HTTP endpoint itself. Store handlers keep [EndpointAuthorize].
#endregion

namespace TimeWarp.Architecture.Features.Applications;

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
      IStore store = arguments.Services?.GetService<IStore>()
        ?? throw new InvalidOperationException($"Tool '{Tool.Name}' needs an IStore on the function arguments.");
      object?[] bound = CatalogAgentArguments.Bind(Tool.Entry, arguments);
      await Tool.Entry.Execute(store, bound, cancellationToken);
      return new CatalogAgentCallResult(Tool.Name, Completed: true);
    }
  }

  private sealed record CatalogAgentCallResult(string Action, bool Completed);
}
