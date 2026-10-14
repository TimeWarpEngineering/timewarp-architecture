#region Purpose
// Task 357: the Ask composer's send cell is the library send button with the arrow-up glyph while
// idle, and Stop (with the send button disabled) while an answer streams; Stop's cancel returns it.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test, the settings menu tests' container shape. The
// composer is the real MessageInput with AskSendButton as its leading action, inside a cascaded
// AgentContext over a real UIAgent. The chat client holds its stream open until cancelled, so the
// conversation stays Streaming for as long as the test needs. HtmlRenderer dispatches no DOM
// events, so a click on Stop and the empty/sendable CSS states are the Playwright Ask surface test;
// here the markup for each conversation status is pinned, and cancel goes through the same
// AgentContext.CancelAsync that Stop calls. Send and cancel run on the renderer's dispatcher:
// MessageInput renders from the conversation's status callback.
#endregion

namespace AskSendButton_;

using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Unit")]
public partial class AskSendButton_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSendButton_Should_>();

  public static async Task Show_The_Arrow_Glyph_Over_An_Enabled_Send_When_Idle()
  {
    using SendSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    using HeldStreamChatClient client = new();
    using UIAgent agent = new(client);
    using AgentContext conversation = new(agent);
    await using HtmlRenderer renderer = new(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());

    HtmlRootComponent composer = await RenderAsync(renderer, conversation);
    string html = await HtmlAsync(renderer, composer);

    string send = SendTag(html);
    send.ShouldContain("aria-label=\"Send message\"");
    send.ShouldNotContain("disabled");
    html.ShouldContain("data-qa=\"AskSendGlyph\"");
    TagOf(html, "AskSendGlyph").ShouldContain("aria-hidden=\"true\"");
    html.ShouldNotContain("data-qa=\"AskStop\"");
  }

  public static async Task Show_Stop_And_Disable_Send_While_Streaming_Then_Return_On_Cancel()
  {
    using SendSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    using HeldStreamChatClient client = new();
    using UIAgent agent = new(client);
    using AgentContext conversation = new(agent);
    await using HtmlRenderer renderer = new(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent composer = await RenderAsync(renderer, conversation);

    // MessageInput renders from the status callback, so the turn runs on the renderer's dispatcher as it does in the app.
    Task turn = Task.CompletedTask;
    // An Action, not a Func<Task>: the dispatcher must start the turn, not await it.
    void Send() => turn = conversation.SendMessageAsync("Hello");
    await renderer.Dispatcher.InvokeAsync(Send);
    await client.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
    string streaming = await WaitForHtmlAsync(renderer, composer, html => html.Contains("data-qa=\"AskStop\"", StringComparison.Ordinal));

    conversation.Status.ShouldBe(ConversationStatus.Streaming);
    string stop = TagOf(streaming, "AskStop");
    stop.ShouldContain("aria-label=\"Stop\"");
    streaming.ShouldNotContain("data-qa=\"AskSendGlyph\"");
    SendTag(streaming).ShouldContain("disabled");

    await renderer.Dispatcher.InvokeAsync(conversation.CancelAsync);
    await turn.WaitAsync(TimeSpan.FromSeconds(10));
    string idle = await WaitForHtmlAsync(renderer, composer, html => html.Contains("data-qa=\"AskSendGlyph\"", StringComparison.Ordinal));

    conversation.Status.ShouldBe(ConversationStatus.Idle);
    idle.ShouldNotContain("data-qa=\"AskStop\"");
    SendTag(idle).ShouldNotContain("disabled");
  }

  private static Task<HtmlRootComponent> RenderAsync(HtmlRenderer renderer, AgentContext conversation)
  {
    RenderFragment leading = builder =>
    {
      builder.OpenComponent<AskSendButton>(0);
      builder.CloseComponent();
    };
    RenderFragment input = builder =>
    {
      builder.OpenComponent<MessageInput>(0);
      builder.AddComponentParameter(1, nameof(MessageInput.LeadingActions), leading);
      builder.CloseComponent();
    };
    ParameterView parameters = ParameterView.FromDictionary
    (
      new Dictionary<string, object?>
      {
        [nameof(CascadingValue<>.Value)] = conversation,
        [nameof(CascadingValue<>.ChildContent)] = input,
      }
    );
    return renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<CascadingValue<AgentContext>>(parameters));
  }

  private static Task<string> HtmlAsync(HtmlRenderer renderer, HtmlRootComponent composer) =>
    renderer.Dispatcher.InvokeAsync(composer.ToHtmlString);

  private static async Task<string> WaitForHtmlAsync(HtmlRenderer renderer, HtmlRootComponent composer, Func<string, bool> ready)
  {
    DateTime deadline = DateTime.UtcNow.AddSeconds(10);
    string html = await HtmlAsync(renderer, composer);
    while (!ready(html))
    {
      DateTime.UtcNow.ShouldBeLessThan(deadline, html);
      await Task.Delay(20);
      html = await HtmlAsync(renderer, composer);
    }

    return html;
  }

  private static string SendTag(string html)
  {
    string? tag = OpeningTag().Matches(html)
      .Select(static match => match.Value)
      .FirstOrDefault(static value => value.Contains("class=\"sc-ai-input__send\"", StringComparison.Ordinal));
    tag.ShouldNotBeNull("sc-ai-input__send");
    return tag;
  }

  private static string TagOf(string html, string dataQa)
  {
    string? tag = OpeningTag().Matches(html)
      .Select(static match => match.Value)
      .FirstOrDefault(value => value.Contains($"data-qa=\"{dataQa}\"", StringComparison.Ordinal));
    tag.ShouldNotBeNull(dataQa);
    return tag;
  }

  [GeneratedRegex("<[^>]+>")]
  private static partial Regex OpeningTag();

  /// <summary>Holds every stream open until its request is cancelled.</summary>
  private sealed class HeldStreamChatClient : IChatClient
  {
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    ) => throw new NotSupportedException(nameof(GetResponseAsync));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      Started.TrySetResult();
      await Task.Delay(Timeout.Infinite, cancellationToken);
      yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
  }

  /// <summary>In-proc SPA container; the composer needs only state and FluentUI.</summary>
  private sealed class SendSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public SendSpa()
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
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new AnonymousAuthenticationStateProvider());
      services.AddScoped<IJSRuntime, NoJsRuntime>();
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

  /// <summary>The composer renders without interop; any JS call is a test failure.</summary>
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
