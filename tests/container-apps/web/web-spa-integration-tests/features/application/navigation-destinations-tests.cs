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
// navigate enum and site_map rows on Home (less Sign in), in the same order. Signed out, Sign in
// is the one extra row on the palette, navigate and site_map, as on Ctrl-K. Every registry page
// the principal may not open, Login while signed in, and an unknown url are refused by navigate.
// Each principal's set is also pinned: anonymous sees only Home, the plain user exactly Home,
// Settings, Profile, AgentLinks and Feedback, the developer no Admin page and no Settings, and the
// administrator every registry page, so a filter that lets everything through fails.
// The guard reads the agent sources (agent/** and page-agent-context.cs): outside comments, no
// string literal may start with a route ("/Letter"), and no typeof(...Page), nameof(...Page) or
// XPage.GetPageUrl/Title/NavIcon/Policy read may appear, so page knowledge stays on [Page] and
// the generated registry. The registry itself is checked for strictly increasing NavOrder, for
// each NavGroup being one contiguous run (else NavMenu draws the category twice), and for
// AgentActions that name catalog actions.
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
using TimeWarp.Architecture.Web.Spa;

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

  public static Task Order_The_Registry_By_NavOrder_And_Name_Catalog_Actions()
  {
    int previous = int.MinValue;
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      entry.NavOrder.ShouldBeGreaterThan(previous, entry.PageType.Name);
      previous = entry.NavOrder;
    }

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

    using DestinationSpa spa = new(null);
    IActionCatalog catalog = spa.ServiceProvider.GetRequiredService<IActionCatalog>();
    PageRegistry.ActionScopes.ShouldNotBeEmpty();
    foreach (PageActionScope scope in PageRegistry.ActionScopes)
    {
      scope.RouteTemplates.ShouldNotBeEmpty(scope.PageType.Name);
      foreach (Type action in scope.ActionTypes)
      {
        catalog.Entries.ShouldContain(entry => entry.ActionType == action, $"{scope.PageType.Name} {action.FullName}");
      }
    }

    PageAgentScope.ActionTypesFor("/Settings").ShouldNotBeEmpty();
    PageAgentScope.ActionTypesFor("/").ShouldBeEmpty();
    PageAgentScope.ActionTypesFor($"/Feedback/{Guid.NewGuid():D}").ShouldNotBeEmpty();
    PageAgentScope.ActionTypesFor("/Feedback/not-a-guid").ShouldBeEmpty();
    PageAgentScope.ActionTypesFor("/FeedbackExtra").ShouldBeEmpty();
    return Task.CompletedTask;
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

    string[] menu = await MenuHrefsAsync(services, user);
    menu.ShouldBe(destinations, because);

    IReadOnlyList<CommandPaletteRow> rows = await CommandPaletteRoster.BuildAsync(user, authorization, PageRegistry.All, catalog.Entries, "/");
    string[] palette = [.. rows.Where(static row => row.Kind == CommandPaletteRowKind.Page).Select(static row => row.Target)];
    string signIn = CommandPaletteRoster.SignInRow("/").Target;
    string[] expected = signedIn ? destinations : [signIn, .. destinations];
    palette.ShouldBe(expected, because);

    IReadOnlyList<CatalogAgentTool> tools = await CatalogAgentToolSet.SelectAsync(user, authorization, catalog.Entries, "/", CancellationToken.None);
    CatalogAgentTool navigate = tools.Single(tool => tool.Name == AgentNavigate.ToolName);
    JsonNode schema = JsonNode.Parse(navigate.InputSchema).ShouldNotBeNull();
    string[] enumerated = [.. schema["properties"]!["url"]!["enum"]!.AsArray().Select(static node => node!.GetValue<string>())];
    enumerated.ShouldBe(expected, because);
    tools.ShouldContain(tool => tool.Name == AgentSiteMap.ToolName && !tool.RequiresApproval, because);

    AgentSiteMap.Result map = await AgentSiteMap.InvokeAsync(user, authorization, scope.Store, "/", CancellationToken.None);
    map.Error.ShouldBeNull(because);
    map.Destinations.Select(static destination => destination.Route).ShouldBe(expected, because);
    foreach (AgentSiteMap.Destination destination in map.Destinations.Where(destination => destination.Route != signIn))
    {
      destination.Description.ShouldNotBeNullOrWhiteSpace($"{because} {destination.Route}");
    }

    List<string> refused = [.. PageRegistry.All.Select(static page => page.Url).Except(destinations), "/not-a-page"];
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
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
      HtmlRootComponent root = await renderer.RenderComponentAsync<CascadingValue<Task<AuthenticationState>>>(parameters);
      await root.QuiescenceTask;
      return root.ToHtmlString();
    });
    return [.. Href().Matches(html).Select(static match => match.Groups[1].Value).Where(static href => href.StartsWith('/', StringComparison.Ordinal))];
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

  [GeneratedRegex(@"\b\w+Page\.(GetPageUrl|Title|NavIcon|Policy)\b")]
  private static partial Regex PageMember();

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
