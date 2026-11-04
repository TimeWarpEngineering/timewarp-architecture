#region Purpose
// Task 300: the navigation menu, the Ctrl-K palette, and the agent's navigate and site_map tools
// list the same destinations for every principal, and no second hand-written page list reappears.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per principal (the palette tests' container shape), no
// Aspire. Four principals: administrator (every permission), plain user (the self-service
// permissions a signed-in person gets), developer, and anonymous. For each, NavMenu is rendered
// with HtmlRenderer under the principal's cascaded authentication state and its internal hrefs
// are read; they must equal NavigationDestinations, the palette's Page rows (less Sign in), the
// navigate enum and site_map rows on Home (less Sign in), in the same order. A principal who can
// open the design system also gets one Page row per catalog part after the pages, on the palette,
// the navigate enum and site_map (task 446); those rows are not menu items. Each site_map part
// row names /DesignSystem as its parent. The navigate description stays under the chat
// contract's tool-description cap and the Ask summary keeps the parts line under SiteMapCap.
// Signed out, Sign in is the one extra row on the palette, navigate and site_map, as on Ctrl-K.
// Every registry page the principal may not open, a part url when the design system is not
// permitted, Login while signed in, and an unknown url are refused by navigate.
// Each principal's set is also pinned: anonymous sees only Home, the plain user exactly Home,
// Settings, Profile, AgentLinks and Feedback, the developer no Admin page and no Settings, and the
// administrator every registry page, so a filter that lets everything through fails.
// The guard reads the agent sources (agent/** and page-agent-context.cs): outside comments, no
// string literal may start with a route ("/Letter"), and no typeof(...Page), nameof(...Page) or
// XPage.GetPageUrl/Title/NavIcon/Policy read may appear, so page knowledge stays on [Page] and
// the generated registry. The registry itself is checked for strictly increasing NavOrder, for
// each NavGroup being one contiguous run (else NavMenu draws the category twice), and for
// AgentActions that name catalog actions. The discovery document publishes a route row only for
// actions whose visibility includes Agent, under the catalog name. WebMCP sections of llms.txt,
// auth.md, and the discovery skill may cite a catalog tool, a short tool id, or a Pascal route
// only when that name is in the document. Backtick words tools, status, and stub are prose.
#endregion

namespace NavigationDestinations_;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;

[TestTag("Unit")]
public partial class Destinations_Should_
{
  private static readonly string[] PlainUser =
  [
    PermissionIds.ProfileRead,
    PermissionIds.SettingsRead,
    PermissionIds.FeedbackFileSelf,
    PermissionIds.AgentLinkManageSelf,
  ];

  private static readonly string[] Developer = [PermissionIds.DeveloperAccess, PermissionIds.DeveloperClaimsRead];

  private static readonly HashSet<string> ProseBackticks = ["tools", "status", "stub"];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Destinations_Should_>();

  public static async Task Match_Across_Menu_Palette_And_Agent_For_Administrator()
  {
    string[] destinations = await AssertSameDestinationsAsync([.. PermissionIds.All], "Administrator");
    destinations.ShouldBe([.. PageRegistry.All.Select(static page => page.Url)]);
  }

  public static async Task Match_Across_Menu_Palette_And_Agent_For_Plain_User()
  {
    string[] destinations = await AssertSameDestinationsAsync(PlainUser, "PlainUser");
    destinations.ShouldBe(["/", "/Settings", "/Profile", "/AgentLinks", "/Feedback"]);
  }

  public static async Task Match_Across_Menu_Palette_And_Agent_For_Developer()
  {
    string[] destinations = await AssertSameDestinationsAsync(Developer, "Developer");
    destinations.ShouldContain("/");
    destinations.ShouldNotContain("/Settings");
    destinations.ShouldAllBe(static url => !url.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase));
  }

  public static async Task Keep_External_In_The_Primary_Nav()
  {
    PageRegistry.All.ShouldAllBe(static entry => entry.NavPlacement.ToString() == "Top");

    using DestinationSpa anonymous = new(null);
    string signedOut = await MenuHtmlAsync(anonymous.ServiceProvider, anonymous.User);
    AssertExternalFollowsHome(signedOut);

    using DestinationSpa administrator = new([.. PermissionIds.All]);
    string signedIn = await MenuHtmlAsync(administrator.ServiceProvider, administrator.User);
    string primary = PrimaryNav(signedIn);
    primary.ShouldContain("/DesignSystem");
    primary.ShouldContain("https://thefreezeteam.com/");
    primary.IndexOf("/DesignSystem", StringComparison.Ordinal)
      .ShouldBeLessThan(primary.IndexOf("https://thefreezeteam.com/", StringComparison.Ordinal));
    signedIn.ShouldNotContain("aria-label=\"Pinned\"");
    signedIn.ShouldNotContain("twe-nav--pin");
  }

  public static async Task Match_Across_Menu_Palette_And_Agent_For_Anonymous()
  {
    string[] destinations = await AssertSameDestinationsAsync(null, "Anonymous");
    destinations.ShouldBe(["/"]);
  }

  public static Task Keep_Page_Knowledge_Out_Of_The_Agent_Sources()
  {
    string agent = RepoFile("source", "container-apps", "web", "projects", "web-spa", "features", "application", "agent");
    List<string> files = [.. Directory.GetFiles(agent, "*.cs", SearchOption.AllDirectories)];
    files.Add(RepoFile("source", "container-apps", "web", "projects", "web-spa", "components", "page-agent-context.cs"));
    files.Count.ShouldBeGreaterThan(10);
    List<string> offenders = [];
    foreach (string file in files)
    {
      int number = 0;
      foreach (string line in File.ReadAllLines(file))
      {
        number++;
        string code = line.TrimStart();
        if (code.StartsWith("//", StringComparison.Ordinal))
        {
          continue;
        }

        if (RouteLiteral().IsMatch(code) || PageTypeOf().IsMatch(code) || PageNameOf().IsMatch(code) || PageMember().IsMatch(code))
        {
          offenders.Add($"{Path.GetFileName(file)}:{number}: {code}");
        }
      }
    }

    offenders.ShouldBeEmpty("Page routes and page types belong on [Page] (NavGroup, NavOrder, Description, AgentActions), not in agent code.");
    return Task.CompletedTask;
  }

  public static async Task Order_The_Registry_By_NavOrder_And_Name_Catalog_Actions()
  {
    int previous = int.MinValue;
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      entry.NavOrder.ShouldBeGreaterThan(previous, entry.PageType.Name);
      previous = entry.NavOrder;
    }

    // Top-level ("") pages draw no category header, so only named groups must be contiguous.
    HashSet<string> closed = [];
    string? current = null;
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      if (entry.NavGroup != current)
      {
        if (current is { Length: > 0 })
        {
          closed.Add(current);
        }

        closed.ShouldNotContain(entry.NavGroup, $"{entry.PageType.Name}: NavOrder splits the {entry.NavGroup} menu category");
        current = entry.NavGroup;
      }
    }

    // One named group uses one band. Bottom is the pinned nav. This registry keeps every group on Top.
    // Compare the name: this suite references two assemblies that each emit NavPlacement.
    Dictionary<string, string> placementByGroup = [];
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      string placement = entry.NavPlacement.ToString();
      if (entry.NavGroup.Length == 0)
      {
        placement.ShouldBe("Top", entry.PageType.Name);
        continue;
      }

      if (placementByGroup.TryGetValue(entry.NavGroup, out string? seen))
      {
        placement.ShouldBe(seen, $"{entry.PageType.Name}: {entry.NavGroup} mixes Top and Bottom");
      }
      else
      {
        placementByGroup[entry.NavGroup] = placement;
      }
    }

    placementByGroup["Developer"].ShouldBe("Top");
    placementByGroup["Admin"].ShouldBe("Top");
    placementByGroup["Demos"].ShouldBe("Top");
    placementByGroup.Values.ShouldAllBe(static placement => placement == "Top");

    using DestinationSpa spa = new(null);
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = spa.ServiceProvider.GetRequiredService<IAuthorizationService>();
    PageRegistry.ActionScopes.ShouldNotBeEmpty();
    List<(string Route, string[] Tools)> expectedRoutes = [];
    foreach (PageActionScope scope in PageRegistry.ActionScopes)
    {
      scope.RouteTemplates.ShouldNotBeEmpty(scope.PageType.Name);
      List<string> tools = [];
      foreach (Type action in scope.ActionTypes)
      {
        ActionCatalogEntry entry = catalog.Entries.Single(candidate => candidate.ActionType == action);
        WebMcpDiscoveryCatalog.CatalogName(action).ShouldBe(entry.Name, action.FullName);
        if (entry.Visibility.HasFlag(ActionVisibility.Agent))
        {
          tools.Add(entry.Name);
        }
      }

      if (tools.Count == 0)
      {
        continue;
      }

      string[] published = [.. tools];
      foreach (string routeTemplate in scope.RouteTemplates)
      {
        expectedRoutes.Add((routeTemplate, published));
      }
    }

    PageAgentScope.ActionTypesFor("/Settings").ShouldNotBeEmpty();
    PageAgentScope.ActionTypesFor("/").ShouldBeEmpty();
    PageAgentScope.ActionTypesFor($"/Feedback/{Guid.NewGuid():D}").ShouldNotBeEmpty();
    PageAgentScope.ActionTypesFor("/Feedback/not-a-guid").ShouldBeEmpty();
    PageAgentScope.ActionTypesFor("/FeedbackExtra").ShouldBeEmpty();

    string json = await WebMcpDiscoveryCatalog.DocumentJsonAsync(spa.User, authorization, catalog.Entries, "/");
    JsonNode document = JsonNode.Parse(json).ShouldNotBeNull();
    document["transport"]!.GetValue<string>().ShouldBe(WebMcpDiscoveryCatalog.Transport);
    document["backendMcp"]!["capabilitiesTools"]!.GetValue<bool>().ShouldBeFalse();
    document["pageLoadTool"]!.GetValue<string>().ShouldBe(AgentNavigate.ToolName);
    string[] everyPage = [.. document["everyPageTools"]!.AsArray().Select(static node => node!.GetValue<string>())];
    everyPage.ShouldBe([AgentNavigate.ToolName, AgentSiteMap.ToolName, PageAgentContext.ToolName]);
    document["inspector"]!["featureDetect"]!.GetValue<string>().ShouldBe(WebMcpDiscoveryCatalog.FeatureDetect);
    document["inspector"]!["polyfill"]!.GetValue<string>().ShouldBe(WebMcpDiscoveryCatalog.Polyfill);
    document["inspector"]!["trace"]!.GetValue<string>().ShouldBe(WebMcpDiscoveryCatalog.Trace);

    JsonArray routes = document["routeScopedTools"]!.AsArray();
    routes.Count.ShouldBe(expectedRoutes.Count);
    for (int index = 0; index < expectedRoutes.Count; index++)
    {
      routes[index]!["routeTemplate"]!.GetValue<string>().ShouldBe(expectedRoutes[index].Route);
      string[] documented = [.. routes[index]!["tools"]!.AsArray().Select(static node => node!.GetValue<string>())];
      documented.ShouldBe(expectedRoutes[index].Tools, expectedRoutes[index].Route);
    }

    string[] publishedTools =
    [
      .. routes.SelectMany(static node => node!["tools"]!.AsArray().Select(static tool => tool!.GetValue<string>()))
    ];
    publishedTools.ShouldContain("Credentials.AddPasskey");
    publishedTools.ShouldNotContain("Credentials.AddExistingPasskey");
    publishedTools.ShouldNotContain("Credentials.LinkMicrosoft365");

    IReadOnlyList<AgentSiteMap.Destination> destinations =
      await AgentSiteMap.DestinationsAsync(spa.User, authorization, "/");
    string[] documentedDestinations =
      [.. document["anonymousDestinations"]!.AsArray().Select(static node => node!["url"]!.GetValue<string>())];
    documentedDestinations.ShouldBe(destinations.Select(static destination => destination.Route).ToArray());

    string pageLoad = await WebMcpDiscoveryCatalog.PageLoadJsonAsync(spa.User, authorization, "/");
    JsonNode load = JsonNode.Parse(pageLoad).ShouldNotBeNull();
    load["name"]!.GetValue<string>().ShouldBe(AgentNavigate.ToolName);
    load["requiresApproval"]!.GetValue<bool>().ShouldBeTrue();
    load["refusal"]!.GetValue<string>().ShouldBe(AgentNavigate.Refusal);
    string[] enumerated = [.. load["inputSchema"]!["properties"]!["url"]!["enum"]!.AsArray().Select(static node => node!.GetValue<string>())];
    enumerated.ShouldBe(documentedDestinations);
  }

  public static async Task WebMcp_Sections_Should_Cite_Only_Published_Tools_And_Routes()
  {
    using DestinationSpa spa = new(null);
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = spa.ServiceProvider.GetRequiredService<IAuthorizationService>();
    string json = await WebMcpDiscoveryCatalog.DocumentJsonAsync(spa.User, authorization, catalog.Entries, "/");
    JsonNode document = JsonNode.Parse(json).ShouldNotBeNull();
    HashSet<string> tools = [document["pageLoadTool"]!.GetValue<string>()];
    foreach (JsonNode? node in document["everyPageTools"]!.AsArray())
    {
      tools.Add(node!.GetValue<string>());
    }

    HashSet<string> routes = [];
    foreach (JsonNode? node in document["routeScopedTools"]!.AsArray())
    {
      routes.Add(node!["routeTemplate"]!.GetValue<string>());
      foreach (JsonNode? tool in node["tools"]!.AsArray())
      {
        tools.Add(tool!.GetValue<string>());
      }
    }

    string[] markdownFiles =
    [
      RepoFile("source", "container-apps", "web", "projects", "web-spa", "wwwroot", "llms.txt"),
      RepoFile("source", "container-apps", "web", "projects", "web-spa", "wwwroot", "auth.md"),
      RepoFile
      (
        "source",
        "container-apps",
        "web",
        "projects",
        "web-spa",
        "wwwroot",
        ".well-known",
        "agent-skills",
        "timewarp-architecture-discovery",
        "SKILL.md"
      ),
    ];

    foreach (string markdownFile in markdownFiles)
    {
      string section = WebMcpSection(File.ReadAllText(markdownFile));
      int citations = 0;
      foreach (Match match in CatalogToolCitation().Matches(section))
      {
        citations++;
        tools.ShouldContain(match.Groups[1].Value, markdownFile);
      }

      foreach (Match match in ShortToolCitation().Matches(section))
      {
        string name = match.Groups[1].Value;
        if (ProseBackticks.Contains(name))
        {
          continue;
        }

        citations++;
        tools.ShouldContain(name, markdownFile);
      }

      foreach (Match match in PascalRouteCitation().Matches(section))
      {
        citations++;
        string cited = match.Groups[1].Value;
        routes.Any
        (
          template => template.Equals(cited, StringComparison.Ordinal)
            || template.StartsWith(cited + "/", StringComparison.Ordinal)
        ).ShouldBeTrue($"{markdownFile} names {cited}");
      }

      citations.ShouldBeGreaterThan(0, markdownFile);
    }
  }
}
