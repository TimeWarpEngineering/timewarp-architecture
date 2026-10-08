#region Purpose
// Builds the current page's WebMCP tool list and asks the browser to replace its previous one.
#endregion

#region Design
// The list is the permission-filtered catalog tools for the route plus page_context, which is
// present even when the route has no catalog tools. Human-only actions never appear.
// Publishing is a store handler's job. The shell only dispatches SyncWebMcp.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Publishes the current page's tools to the browser model context.</summary>
public sealed class WebMcpPublisher
{
  private readonly JsWebMcpModelContext Context;
  private readonly IActionCatalog Catalog;
  private readonly IAuthorizationService AuthorizationService;
  private readonly AuthenticationStateProvider AuthenticationStateProvider;
  private readonly NavigationManager Navigation;

  public WebMcpPublisher
  (
    JsWebMcpModelContext context,
    IActionCatalog catalog,
    IAuthorizationService authorizationService,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigation
  )
  {
    Context = context;
    Catalog = catalog;
    AuthorizationService = authorizationService;
    AuthenticationStateProvider = authenticationStateProvider;
    Navigation = navigation;
  }

  public static async Task<IReadOnlyList<WebMcpToolDescriptor>> DescribeAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IActionCatalog catalog,
    string? path,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(catalog);
    IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
    (
      user,
      authorizationService,
      catalog.Entries,
      path,
      cancellationToken
    );

    List<WebMcpToolDescriptor> tools = [];
    foreach (CatalogAgentTool tool in selected)
    {
      tools.Add(new WebMcpToolDescriptor(tool.Name, tool.Description, tool.InputSchema));
    }

    tools.Add
    (
      new WebMcpToolDescriptor
      (
        PageAgentContext.ToolName,
        PageAgentContext.ToolDescription,
        PageAgentContext.EmptyInputSchema
      )
    );
    return tools;
  }

  public async Task PublishAsync(CancellationToken cancellationToken)
  {
    // Re-read on every publish. The publisher does not cache an AuthenticationState.
#pragma warning disable BL0013
    AuthenticationState authentication = await AuthenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
    string path = PageAgentScope.FromNavigation(Navigation);
    IReadOnlyList<WebMcpToolDescriptor> tools = await DescribeAsync
    (
      authentication.User,
      AuthorizationService,
      Catalog,
      path,
      cancellationToken
    );
    await WebMcpRegistration.ApplyAsync(Context, tools, cancellationToken);
  }
}
