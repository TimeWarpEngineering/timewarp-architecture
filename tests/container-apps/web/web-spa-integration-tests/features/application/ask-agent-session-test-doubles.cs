#region Purpose
// In-proc host and scripted chat clients for the Ask session remount proofs.
#endregion

#region Design
// SessionHost is the readiness host plus AskAgentSession, a scripted chat configuration, and
// dictionary local storage so the panel's first render can dismiss nothing. RemountHost hides
// and shows AgentAsk when RemountSignal changes, which is the TimeWarpPage dispose and remount.
// NavigatingChatClient's reply waits until the test has remounted. HeldChatClient stays open
// until the conversation cancels the stream. This file is a project test, not a standalone
// runfile: the integration project compiles it, and CI runs that project.
#endregion

namespace AskAgentSession_;

using System.Runtime.CompilerServices;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

public partial class AskAgentSession_Should_
{
  private sealed class RemountSignal
  {
    private Action? Changed;

    public bool Show { get; private set; } = true;

    public void Listen(Action onChanged) => Changed += onChanged;

    public void Forget(Action onChanged) => Changed -= onChanged;

    public void Set(bool show)
    {
      Show = show;
      Changed?.Invoke();
    }
  }

  private sealed class RemountHost : ComponentBase, IDisposable
  {
    [Inject] public RemountSignal Signal { get; set; } = null!;

    [Parameter] public Action<AgentAsk?>? Capture { get; set; }

    protected override void OnInitialized()
    {
      Signal.Listen(RequestRender);
    }

    public void Dispose() => Signal.Forget(RequestRender);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      if (!Signal.Show)
      {
        Capture?.Invoke(null);
        return;
      }

      builder.OpenComponent<AgentAsk>(0);
      builder.AddComponentReferenceCapture(1, instance => Capture?.Invoke((AgentAsk)instance));
      builder.CloseComponent();
    }

    private void RequestRender() => _ = InvokeAsync(StateHasChanged);
  }

  /// <summary>First answer calls navigate; the reply waits until the test remounts the panel.</summary>
  private sealed class NavigatingChatClient : IChatClient
  {
    private readonly TaskCompletionSource ToolResultReachedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource Remounted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int Phase;

    public Task ToolResultReached => ToolResultReachedSource.Task;

    public int NavigateOffers { get; private set; }

    public IReadOnlyList<ChatMessage>? SecondTurn { get; private set; }

    public void Continue() => Remounted.TrySetResult();

    public async Task<ChatResponse> GetResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default)
    {
      _ = options;
      List<ChatMessage> list = [.. messages];
      cancellationToken.ThrowIfCancellationRequested();
      if (Phase == 0)
      {
        Phase = 1;
        NavigateOffers++;
        FunctionCallContent call = new(
          "call-navigate",
          AgentNavigate.ToolName,
          new Dictionary<string, object?> { ["url"] = "/Admin/Roles/New" });
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, [call]));
      }

      if (Phase == 1)
      {
        Phase = 2;
        ToolResultReachedSource.TrySetResult();
        await Remounted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Opened the new role page."));
      }

      SecondTurn = list;
      return new ChatResponse(new ChatMessage(ChatRole.Assistant, "The new role page is open."));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
      foreach (ChatMessage message in response.Messages)
      {
        yield return new ChatResponseUpdate(message.Role, message.Contents);
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
  }

  /// <summary>Holds the stream open until the conversation cancels it.</summary>
  private sealed class HeldChatClient : IChatClient
  {
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<ChatResponse> GetResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default) =>
      throw new NotSupportedException(nameof(GetResponseAsync));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      _ = messages;
      _ = options;
      Started.TrySetResult();
      try
      {
        await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        Cancelled.TrySetResult();
        throw;
      }

      yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
  }

  private sealed class SessionHost : IDisposable
  {
    private readonly ServiceProvider Provider;

    public RecordingRuntime Runtime { get; } = new();

    public SessionHost(IChatClient client)
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
          .. PermissionIds.All.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
        ],
        authenticationType: "test"));
      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState(options =>
      {
        options.Assemblies =
        [
          typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
          typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly,
        ];
      });
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddBlazoredLocalStorage();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      services.AddScoped<IJSRuntime>(_ => Runtime);
      services.AddScoped(provider => new AskDiagnostics(isDevelopment: true, provider.GetRequiredService<IJSRuntime>()));
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<PageAgentRoute>();
      services.AddScoped<AgentCallOutcome>();
      services.AddScoped<AskConversationThreads>();
      services.AddScoped<AskAgentSession>();
      services.AddScoped<RemountSignal>();
      services.AddScoped<IWebServerApiService>(_ => new ScriptedServer());
      services.AddSingleton<IChatClient>(client);
      Provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    public void Dispose() => Provider.Dispose();
  }

  private sealed class ScriptedServer : IWebServerApiService
  {
    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(
      IApiRequest request,
      CancellationToken cancellationToken) where TResponse : class
    {
      _ = request;
      _ = cancellationToken;
      if (typeof(TResponse) == typeof(GetAgentChatConfiguration.Response))
      {
        GetAgentChatConfiguration.Response response = new()
        {
          Configured = true,
          SetupCommand = XaiChatDefaults.SetupCommand,
          Model = "grok-4.7",
        };
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(
          (TResponse)(object)response);
      }

      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(
        new SharedProblemDetails { Status = 500, Title = "No scripted outcome" });
    }
  }

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }

  private sealed class RecordingRuntime : IJSRuntime
  {
    public List<(string Identifier, object?[] Arguments)> Calls { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      Calls.Add((identifier, args ?? []));
      if (string.Equals(identifier, "import", StringComparison.Ordinal)
        && typeof(TValue).IsAssignableFrom(typeof(NoopModule)))
      {
        return ValueTask.FromResult((TValue)(object)new NoopModule());
      }

      return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      _ = cancellationToken;
      Calls.Add((identifier, args ?? []));
      if (string.Equals(identifier, "import", StringComparison.Ordinal)
        && typeof(TValue).IsAssignableFrom(typeof(NoopModule)))
      {
        return ValueTask.FromResult((TValue)(object)new NoopModule());
      }

      return ValueTask.FromResult<TValue>(default!);
    }
  }

  private sealed class NoopModule : IJSObjectReference
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      _ = identifier;
      _ = args;
      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      _ = identifier;
      _ = cancellationToken;
      _ = args;
      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
