#region Purpose
// Task 348: Ask's empty-state starter prompts bind from XAI:StarterPrompts, normalize to at most six
// usable cards, fall back to the template defaults, and render icon, title, subtitle, and prompt.
#endregion

#region Design
// Binding is the real configuration binder over an in-memory XAI section, the shape an app puts in
// appsettings. The SPA side is the banner tests' container shape: the real LoadChatConfiguration
// handler against a scripted IWebServerApiService, then AskStarterPrompts rendered with
// HtmlRenderer. The renderer has no AgentContext, so a click would do nothing; the prompt text
// each card sends is asserted through its data-prompt attribute.
#endregion

namespace AskStarterPrompts_;

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

[TestTag("Unit")]
public class AskStarterPrompts_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskStarterPrompts_Should_>();

  public static Task Ship_Four_To_Six_Complete_Defaults_With_Known_Icons()
  {
    IReadOnlyList<AgentChatStarterPrompt> defaults = AgentChatStarterPrompts.Defaults;

    defaults.Count.ShouldBeInRange(4, AgentChatStarterPrompts.MaxCount);
    foreach (AgentChatStarterPrompt prompt in defaults)
    {
      prompt.Title.ShouldNotBeNullOrWhiteSpace();
      prompt.Subtitle.ShouldNotBeNullOrWhiteSpace();
      prompt.Prompt.ShouldNotBeNullOrWhiteSpace();
      AskStarterPromptIcons.Names.ShouldContain(prompt.Icon);
    }

    AgentChatStarterPrompts.Normalize(defaults).ShouldBe(defaults);
    return Task.CompletedTask;
  }

  public static Task Bind_StarterPrompts_From_The_XAI_Section()
  {
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection
      (
        new Dictionary<string, string?>
        {
          ["XAI:StarterPrompts:0:Icon"] = "Search",
          ["XAI:StarterPrompts:0:Title"] = "Find an order",
          ["XAI:StarterPrompts:0:Subtitle"] = "Look one up by number",
          ["XAI:StarterPrompts:0:Prompt"] = "Help me find an order.",
          ["XAI:StarterPrompts:1:Title"] = "Draft a refund",
          ["XAI:StarterPrompts:1:Prompt"] = "Start a refund for me.",
        }
      )
      .Build();
    XaiChatOptions options = new();
    configuration.GetSection(XaiChatOptions.SectionName).Bind(options);

    IReadOnlyList<AgentChatStarterPrompt> prompts = AgentChatStarterPrompts.Normalize(options.StarterPrompts);

    prompts.Count.ShouldBe(2);
    prompts[0].Icon.ShouldBe("Search");
    prompts[0].Title.ShouldBe("Find an order");
    prompts[0].Subtitle.ShouldBe("Look one up by number");
    prompts[0].Prompt.ShouldBe("Help me find an order.");
    prompts[1].Icon.ShouldBeNull();
    prompts[1].Subtitle.ShouldBeNull();
    AgentChatStarterPrompts.Normalize(new XaiChatOptions().StarterPrompts).ShouldBe(AgentChatStarterPrompts.Defaults);
    return Task.CompletedTask;
  }

  public static Task Drop_Incomplete_Entries_Trim_And_Cap_At_Six()
  {
    List<AgentChatStarterPrompt?> configured =
    [
      null,
      new() { Title = "  ", Prompt = "No title" },
      new() { Title = "No prompt", Prompt = " " },
    ];
    for (int index = 0; index < 8; index++)
    {
      configured.Add(new() { Icon = "  ", Title = $"  Card {index} ", Subtitle = " ", Prompt = $" Prompt {index} " });
    }

    IReadOnlyList<AgentChatStarterPrompt> prompts = AgentChatStarterPrompts.Normalize(configured);

    prompts.Count.ShouldBe(AgentChatStarterPrompts.MaxCount);
    prompts[0].Title.ShouldBe("Card 0");
    prompts[0].Prompt.ShouldBe("Prompt 0");
    prompts[0].Icon.ShouldBeNull();
    prompts[0].Subtitle.ShouldBeNull();
    prompts[^1].Title.ShouldBe("Card 5");
    AgentChatStarterPrompts.Normalize([null, new() { Title = "x" }]).ShouldBe(AgentChatStarterPrompts.Defaults);
    AgentChatStarterPrompts.Normalize(null).ShouldBe(AgentChatStarterPrompts.Defaults);
    return Task.CompletedTask;
  }

  public static Task Map_Icon_Names_Case_Insensitively_With_A_Default()
  {
    AskStarterPromptIcons.For("navigation").GetType().ShouldBe(typeof(Icons.Regular.Size20.Navigation));
    AskStarterPromptIcons.For(" ShieldCheckmark ").GetType().ShouldBe(typeof(Icons.Regular.Size20.ShieldCheckmark));
    AskStarterPromptIcons.For(null).GetType().ShouldBe(typeof(Icons.Regular.Size20.Sparkle));
    AskStarterPromptIcons.For("NoSuchIcon").GetType().ShouldBe(typeof(Icons.Regular.Size20.Sparkle));
    foreach (string name in AskStarterPromptIcons.Names)
    {
      bool isDefault = name == AskStarterPromptIcons.DefaultName;
      (AskStarterPromptIcons.For(name).GetType() == typeof(Icons.Regular.Size20.Sparkle)).ShouldBe(isDefault, name);
    }

    return Task.CompletedTask;
  }

  public static async Task Render_The_Configured_Prompts_From_The_Server()
  {
    GetAgentChatConfiguration.Response response = Response();
    response.StarterPrompts =
    [
      new() { Icon = "Search", Title = "Find an order", Subtitle = "Look one up by number", Prompt = "Help me find an order." },
      new() { Title = "Draft a refund", Prompt = "Start a refund for me." },
    ];
    using StarterSpa spa = new(response);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());

    string html = await RenderAsync(scope);

    CountOf(html, "data-qa=\"AskStarterPrompt\"").ShouldBe(2);
    html.ShouldContain("Find an order");
    html.ShouldContain("Look one up by number");
    html.ShouldContain("data-prompt=\"Help me find an order.\"");
    html.ShouldContain("data-prompt=\"Start a refund for me.\"");
    CountOf(html, "data-qa=\"AskStarterPromptSubtitle\"").ShouldBe(1);
    CountOf(html, "<svg").ShouldBe(2);
  }

  public static async Task Render_The_Defaults_When_The_Server_Sends_None_Or_Fails()
  {
    GetAgentChatConfiguration.Response empty = Response();
    empty.StarterPrompts = [];
    foreach (StarterSpa spa in new[] { new StarterSpa(empty), new StarterSpa(response: null) })
    {
      using (spa)
      using (SpaTestScope scope = SpaTestScope.Create(spa))
      {
        await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());

        scope.Store.GetState<AgentSurfaceState>().StarterPrompts.ShouldBe(AgentChatStarterPrompts.Defaults);
        string html = await RenderAsync(scope);
        CountOf(html, "data-qa=\"AskStarterPrompt\"").ShouldBe(AgentChatStarterPrompts.Defaults.Count);
        foreach (AgentChatStarterPrompt prompt in AgentChatStarterPrompts.Defaults)
        {
          html.ShouldContain(prompt.Title);
          html.ShouldContain(prompt.Subtitle!);
        }
      }
    }
  }

  private static int CountOf(string html, string fragment) =>
    html.Split(fragment).Length - 1;

  private static GetAgentChatConfiguration.Response Response() =>
    new()
    {
      Configured = true,
      SetupCommand = XaiChatDefaults.SetupCommand,
      Model = "grok-4.7",
    };

  private static async Task<string> RenderAsync(SpaTestScope scope)
  {
    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AskStarterPrompts>()).ToHtmlString());
  }

  /// <summary>Answers with the configured chat settings, or a 500 when there are none.</summary>
  private sealed class ScriptedWebServer(GetAgentChatConfiguration.Response? response) : IWebServerApiService
  {
    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      if (response is null)
      {
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>
        (
          new SharedProblemDetails { Status = 500, Title = "Server error" }
        );
      }

      OneOf<TResponse, FileResponse, SharedProblemDetails> result = response as TResponse
        ?? throw new InvalidOperationException($"No scripted outcome for {typeof(TResponse).Name}.");
      return Task.FromResult(result);
    }
  }

  /// <summary>In-proc SPA container with a scripted web-server API.</summary>
  private sealed class StarterSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public StarterSpa(GetAgentChatConfiguration.Response? response)
    {
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
      services.AddBlazoredLocalStorage();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new AnonymousAuthenticationStateProvider());
      services.AddScoped<IJSRuntime, NoJsRuntime>();
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<IWebServerApiService>(_ => new ScriptedWebServer(response));

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

  /// <summary>The cards render without interop; any JS call is a test failure.</summary>
  private sealed class NoJsRuntime : IJSRuntime
  {
    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      throw new NotSupportedException(identifier);
  }

  private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
  }
}
