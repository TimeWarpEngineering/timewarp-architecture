#region Purpose
// Shared private helpers for the parity tests in the CatalogAgent_Should partial class.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Features.Applications;

public partial class CatalogAgent_Should
{

  private static async Task AssertSameSurfaceAsync
  (
    ClaimsPrincipal principal,
    IAuthorizationService authorization,
    IActionCatalog catalog,
    string route,
    AgentEditMode mode
  )
  {
    IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
    (
      principal,
      authorization,
      catalog.Entries,
      route,
      mode,
      CancellationToken.None
    );
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(selected);
    IReadOnlyList<WebMcpToolDescriptor> web = await WebMcpPublisher.DescribeAsync
    (
      principal,
      authorization,
      catalog,
      route,
      mode,
      CancellationToken.None
    );
    string because = $"{route} {mode}";
    functions.Tools.Count.ShouldBe(web.Count, because);
    for (int index = 0; index < web.Count; index++)
    {
      AITool aiTool = functions.Tools[index];
      WebMcpToolDescriptor mcp = web[index];
      aiTool.Name.ShouldBe(mcp.Name, because);
      aiTool.Description.ShouldBe(mcp.Description, because);
      (aiTool is ApprovalRequiredAIFunction).ShouldBe(mcp.RequiresApproval, $"{because} {mcp.Name}");
      AIFunction function = aiTool.ShouldBeAssignableTo<AIFunction>();
      using JsonDocument aiSchema = JsonDocument.Parse(function.JsonSchema.GetRawText());
      using JsonDocument mcpSchema = JsonDocument.Parse(mcp.InputSchemaJson);
      JsonElement.DeepEquals(aiSchema.RootElement, mcpSchema.RootElement)
        .ShouldBeTrue($"{because} {mcp.Name}");
    }
  }

  private static async Task RememberConversationAsync
  (
    SpaTestScope scope,
    AgentSurfaceState surface,
    IReadOnlyList<string> scopes,
    bool expired
  )
  {
    AuthenticationState authentication = await scope.ServiceProvider
      .GetRequiredService<AuthenticationStateProvider>()
      .GetAuthenticationStateAsync();
    Guid principalId = Guid.Parse(authentication.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    DateTimeOffset utcNow = expired ? FutureNoon().AddDays(-3) : FutureNoon();
    AgentConversationCredential credential = AgentConversationCredentialIssuer.Issue
    (
      principalId,
      scopes,
      utcNow,
      TimeSpan.FromHours(1)
    );
    await surface.SetConversation
    (
      credential.Id,
      credential.PrincipalId,
      [.. credential.Scopes],
      credential.ExpiresAt,
      credential.DisplayName
    );
  }

  private static async Task<IReadOnlyList<CatalogAgentTool>> SelectForSessionAsync
  (
    SpaTestScope scope,
    AgentEditMode mode
  )
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
      mode,
      CancellationToken.None
    );
  }

  /// <summary>Mid-day tomorrow (UTC): a credential issued then is valid now and far from any midnight.</summary>
  private static DateTimeOffset FutureNoon() =>
    new(DateTimeOffset.UtcNow.UtcDateTime.Date.AddDays(1).AddHours(12), TimeSpan.Zero);

  private static AIFunctionArguments Arguments(SpaTestScope scope, string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    Dictionary<string, object?> values = [];
    foreach (JsonProperty property in document.RootElement.EnumerateObject())
    {
      values[property.Name] = property.Value.Clone();
    }

    return new AIFunctionArguments(values) { Services = scope.ServiceProvider };
  }

  private static void ShouldReport(string json, string message)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    document.RootElement.GetProperty("error").GetString().ShouldBe(message);
  }
}
