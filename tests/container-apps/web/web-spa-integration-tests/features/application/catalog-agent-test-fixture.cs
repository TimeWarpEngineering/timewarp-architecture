#region Purpose
// Shared private helpers for the CatalogAgent_Should partial class: page-context invoke, pending wait, scripted approval loop, expected names.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;

[TestTag("Integration")]
public partial class CatalogAgent_Should
{
  private static async Task<string> InvokePageContextAsync(IServiceProvider services)
  {
    PageContextEchoClient echo = new();
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create([]);
    using FunctionInvokingChatClient client = CatalogAgentSession.CreateInvokingClient(echo, services);
    ChatOptions options = new()
    {
      Tools = [.. functions.Tools],
    };
    await client.GetResponseAsync
    (
      [new ChatMessage(ChatRole.User, "Explain this page")],
      options,
      CancellationToken.None
    );
    return echo.ToolResult.ShouldNotBeNull();
  }

  private static string RepoFile(params string[] parts)
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return Path.Combine([directory, .. parts]);
  }

  private static async Task<Guid> WaitForPendingAsync(SpaTestScope scope, Task<string> invoke)
  {
    DateTime started = DateTime.UtcNow;
    while (scope.Store.GetState<AgentSurfaceState>().PendingCallId is null)
    {
      if (invoke.IsCompleted)
      {
        throw new ShouldAssertException($"Invoke finished without waiting for approval: {await invoke}");
      }

      if (DateTime.UtcNow - started > Timeout)
      {
        throw new ShouldAssertException("No approval became pending.");
      }

      await Task.Delay(20);
    }

    return scope.Store.GetState<AgentSurfaceState>().PendingCallId!.Value;
  }

  private static async Task<IReadOnlyList<CatalogAgentTool>> SelectForSessionAsync(SpaTestScope scope)
  {
    AuthenticationState authentication = await scope.ServiceProvider
      .GetRequiredService<AuthenticationStateProvider>()
      .GetAuthenticationStateAsync();
    return await CatalogAgentToolSet.SelectAsync
    (
      authentication.User,
      scope.ServiceProvider.GetRequiredService<IAuthorizationService>(),
      scope.ServiceProvider.GetRequiredService<IActionCatalog>().Entries,
      PageAgentScope.FromNavigation(scope.ServiceProvider.GetRequiredService<NavigationManager>()),
      CancellationToken.None
    );
  }

  /// <summary>Scripted approval loop over the product's invoking client (test-only).</summary>
  private static async Task<string> RunAsync
  (
    IChatClient inner,
    IServiceProvider services,
    ChatOptions options,
    Func<ToolApprovalRequestContent, bool> approve
  )
  {
    const int maximumApprovalRounds = 8;
    using FunctionInvokingChatClient client = CatalogAgentSession.CreateInvokingClient(inner, services);
    List<ChatMessage> history = [new ChatMessage(ChatRole.User, "Add 5 to the counter")];
    for (int round = 0; round < maximumApprovalRounds; round++)
    {
      ChatResponse response = await client.GetResponseAsync(history, options, CancellationToken.None);
      history.AddRange(response.Messages);

      List<AIContent> decisions = [];
      foreach (ChatMessage message in response.Messages)
      {
        foreach (AIContent content in message.Contents)
        {
          if (content is ToolApprovalRequestContent request)
          {
            decisions.Add(request.CreateResponse(approve(request)));
          }
        }
      }

      if (decisions.Count == 0)
      {
        return response.Text ?? "";
      }

      history.Add(new ChatMessage(ChatRole.User, decisions));
    }

    throw new InvalidOperationException($"The catalog agent stopped after {maximumApprovalRounds} approval rounds.");
  }

  private static async Task<string[]> ExpectedNamesAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorization,
    IActionCatalog catalog,
    string path
  )
  {
    string normalized = PageAgentScope.Normalize(path);
    IReadOnlyList<CommandPaletteRow> rows = await CommandPaletteRoster.BuildAsync
    (
      user,
      authorization,
      PageRegistry.All,
      catalog.Entries,
      normalized
    );
    Dictionary<string, ActionCatalogEntry> byName = [];
    foreach (ActionCatalogEntry entry in catalog.Entries)
    {
      byName[entry.Name] = entry;
    }

    List<string> names = [];
    HashSet<string> seen = [];
    foreach (CommandPaletteRow row in rows)
    {
      if (row.Kind != CommandPaletteRowKind.Command
        || !byName.TryGetValue(row.Target, out ActionCatalogEntry? entry)
        || !entry.Visibility.HasFlag(ActionVisibility.Agent)
        || !seen.Add(entry.Name))
      {
        continue;
      }

      names.Add(entry.Name);
    }

    IReadOnlyList<Type> pageActions = user.Identity?.IsAuthenticated == true
      ? PageAgentScope.ActionTypesFor(normalized)
      : [];
    foreach (Type actionType in pageActions)
    {
      ActionCatalogEntry? entry = catalog.Entries.FirstOrDefault(candidate => candidate.ActionType == actionType);
      if (entry is null || !seen.Add(entry.Name) || !entry.Visibility.HasFlag(ActionVisibility.Agent))
      {
        continue;
      }

      if (await CommandPaletteRoster.IsPermittedAsync(user, authorization, entry))
      {
        names.Add(entry.Name);
      }
    }

    names.Add(AgentNavigate.ToolName);
    names.Add(AgentSiteMap.ToolName);
    return [.. names];
  }

  private static async Task<string[]> ExpectedWebMcpNamesAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorization,
    IActionCatalog catalog,
    string path
  )
  {
    string[] names = await ExpectedNamesAsync(user, authorization, catalog, path);
    return [.. names, PageAgentContext.ToolName];
  }

  private static ClaimsPrincipal Principal(params string[] permissions)
  {
    List<Claim> claims = [new Claim(ClaimTypes.Name, "catalog-agent")];
    foreach (string permission in permissions)
    {
      claims.Add(new Claim(PermissionIds.ClaimType, permission));
    }

    return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
  }

  private static string[] Names(IReadOnlyList<CatalogAgentTool> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  private static string[] Names(IReadOnlyList<WebMcpToolDescriptor> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }
}
