#region Purpose
// Shared private helpers, generated regexes, and the DestinationSpa double for the Destinations_Should_ partial class.
#endregion

namespace NavigationDestinations_;

using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FakeItEasy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.DesignSystem;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Unit")]
public partial class Destinations_Should_
{

  private static string WebMcpSection(string markdown)
  {
    const string heading = "## WebMCP";
    int start = markdown.IndexOf(heading, StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0);
    int next = markdown.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
    return next < 0 ? markdown[start..] : markdown[start..next];
  }

  private static async Task<string[]> AssertSameDestinationsAsync(string[]? permissions, string because)
  {
    using DestinationSpa spa = new(permissions);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    IServiceProvider services = scope.ServiceProvider;
    IAuthorizationService authorization = services.GetRequiredService<IAuthorizationService>();
    IActionCatalog catalog = services.GetRequiredService<IActionCatalog>();
    ClaimsPrincipal user = spa.User;
    bool signedIn = permissions is not null;

    string[] destinations = [.. (await NavigationDestinations.ForAsync(user, authorization)).Select(static page => page.Url)];
    destinations.ShouldContain("/", because);
    destinations.ShouldNotContain(
      static url => url.StartsWith("/DesignSystem/", StringComparison.OrdinalIgnoreCase),
      because);
    destinations.Count(static url => string.Equals(url, "/DesignSystem", StringComparison.OrdinalIgnoreCase))
      .ShouldBeLessThanOrEqualTo(1, because);

    string[] menu = await MenuHrefsAsync(services, user);
    menu.ShouldBe(destinations, because);

    IReadOnlyList<CommandPaletteRow> rows = await CommandPaletteRoster.BuildAsync(user, authorization, PageRegistry.All, catalog.Entries, "/");
    string[] palette = [.. rows.Where(static row => row.Kind == CommandPaletteRowKind.Page).Select(static row => row.Target)];
    string signIn = CommandPaletteRoster.SignInRow("/").Target;
    string[] expected = signedIn ? destinations : [signIn, .. destinations];
    string[] partUrls = await NavigationDestinations.IsAuthorizedAsync(user, authorization, PermissionIds.DeveloperAccess)
      ? [.. DesignSystemParts.PageOrder.Select(static part => DesignSystemParts.Url(part))]
      : [];
    palette.ShouldBe([.. expected, .. partUrls], because);

    IReadOnlyList<CatalogAgentTool> tools = await CatalogAgentToolSet.SelectAsync(user, authorization, catalog.Entries, "/", CancellationToken.None);
    CatalogAgentTool navigate = tools.Single(tool => tool.Name == AgentNavigate.ToolName);
    JsonNode schema = JsonNode.Parse(navigate.InputSchema).ShouldNotBeNull();
    string[] enumerated = [.. schema["properties"]!["url"]!["enum"]!.AsArray().Select(static node => node!.GetValue<string>())];
    enumerated.ShouldBe([.. expected, .. partUrls], because);
    navigate.Description.Length.ShouldBeLessThanOrEqualTo(CompleteAgentChat.MaxToolDescriptionLength, because);
    tools.ShouldContain(tool => tool.Name == AgentSiteMap.ToolName && !tool.RequiresApproval, because);

    AgentSiteMap.Result map = await AgentSiteMap.InvokeAsync(user, authorization, scope.Store, "/", CancellationToken.None);
    map.Error.ShouldBeNull(because);
    map.Destinations.Select(static destination => destination.Route).ShouldBe([.. expected, .. partUrls], because);
    map.Destinations.Where(static destination => destination.Parent is not null).Select(static destination => destination.Route)
      .ShouldBe(partUrls, because);
    map.Destinations.Where(static destination => destination.Parent is not null)
      .ShouldAllBe(static destination => destination.Parent == "/DesignSystem", because);
    string summary = AgentSiteMap.Summary(map.Destinations, AgentAskInstructions.SiteMapCap);
    if (partUrls.Length > 0)
    {
      summary.ShouldContain("/DesignSystem/<part> for Design System parts: brand,", customMessage: because);
      summary.ShouldContain(", filter-bar,", customMessage: because);
    }

    foreach (AgentSiteMap.Destination destination in map.Destinations.Where(destination => destination.Route != signIn))
    {
      destination.Description.ShouldNotBeNullOrWhiteSpace($"{because} {destination.Route}");
    }

    List<string> refused = [.. PageRegistry.All.Select(static page => page.Url).Except(destinations), "/not-a-page"];
    if (partUrls.Length == 0)
    {
      refused.Add("/DesignSystem/drawers");
    }

    if (signedIn)
    {
      refused.Add(signIn);
    }

    foreach (string url in refused)
    {
      AgentNavigate.Result result = await AgentNavigate.InvokeAsync
      (
        user,
        authorization,
        scope.Store,
        "/",
        new Dictionary<string, object?> { ["url"] = url },
        CancellationToken.None
      );
      result.Navigated.ShouldBeFalse($"{because} {url}");
      result.Error.ShouldBe(AgentNavigate.Refusal, $"{because} {url}");
    }

    Console.WriteLine($"DESTINATIONS-PROOF {because} menu={string.Join(",", menu)}");
    return destinations;
  }

  private static async Task<string[]> MenuHrefsAsync(IServiceProvider services, ClaimsPrincipal user)
  {
    string html = await MenuHtmlAsync(services, user);
    return [.. Href().Matches(html).Select(static match => match.Groups[1].Value).Where(static href => href.StartsWith('/', StringComparison.Ordinal))];
  }

  private static async Task<string> MenuHtmlAsync(IServiceProvider services, ClaimsPrincipal user)
  {
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    RenderFragment menu = builder =>
    {
      builder.OpenComponent<NavMenu>(0);
      builder.CloseComponent();
    };
    ParameterView parameters = ParameterView.FromDictionary
    (
      new Dictionary<string, object?>
      {
        [nameof(CascadingValue<>.Value)] = Task.FromResult(new AuthenticationState(user)),
        [nameof(CascadingValue<>.ChildContent)] = menu,
      }
    );
    return await renderer.Dispatcher.InvokeAsync(async () =>
    {
      HtmlRootComponent root = await renderer.RenderComponentAsync<CascadingValue<Task<AuthenticationState>>>(parameters);
      await root.QuiescenceTask;
      return root.ToHtmlString();
    });
  }

  private static void AssertExternalFollowsHome(string html)
  {
    string primary = PrimaryNav(html);
    primary.ShouldContain("href=\"/\"");
    primary.ShouldContain("https://thefreezeteam.com/");
    primary.ShouldContain("https://github.com/TimeWarpEngineering/timewarp-architecture");
    primary.IndexOf("href=\"/\"", StringComparison.Ordinal)
      .ShouldBeLessThan(primary.IndexOf("https://thefreezeteam.com/", StringComparison.Ordinal));
    html.ShouldNotContain("aria-label=\"Pinned\"");
    html.ShouldNotContain("twe-nav--pin");
  }

  private static string PrimaryNav(string html)
  {
    const string label = "aria-label=\"Primary\"";
    int start = html.IndexOf(label, StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0);
    int close = html.IndexOf("</nav>", start, StringComparison.Ordinal);
    close.ShouldBeGreaterThan(start);
    return html[start..close];
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

  [GeneratedRegex("href=\"([^\"]*)\"")]
  private static partial Regex Href();

  [GeneratedRegex("""(?<![\w])"/[A-Za-z]""")]
  private static partial Regex RouteLiteral();

  [GeneratedRegex(@"typeof\(\s*[\w.]*Page\s*\)")]
  private static partial Regex PageTypeOf();

  [GeneratedRegex(@"nameof\(\s*[\w.]*Page\s*\)")]
  private static partial Regex PageNameOf();

  [GeneratedRegex(@"\b[A-Z]\w*Page\.(GetPageUrl|Title|NavIcon|Policy)\b")]
  private static partial Regex PageMember();

  [GeneratedRegex("`([A-Z][A-Za-z0-9]*\\.[A-Z][A-Za-z0-9]*)`")]
  private static partial Regex CatalogToolCitation();

  [GeneratedRegex("`([a-z][a-z0-9_]*)`")]
  private static partial Regex ShortToolCitation();

  [GeneratedRegex(@"(?<![\w./])(/[A-Z][A-Za-z0-9]*)\b")]
  private static partial Regex PascalRouteCitation();

  /// <summary>In-proc SPA container for one principal (null = signed out).</summary>
  private sealed class DestinationSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public ClaimsPrincipal User { get; }

    public DestinationSpa(string[]? permissions)
    {
      User = permissions is null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(
          [
            new Claim("sub", Guid.NewGuid().ToString()),
            .. permissions.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
          ],
          authenticationType: "test"));

      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(User));
      services.AddScoped(_ => A.Fake<IJSRuntime>());
      services.AddScoped<NavigationManager, TestNavigationManager>();

      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }
}
