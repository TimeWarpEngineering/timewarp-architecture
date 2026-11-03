#region Purpose
// Nested test doubles for the CatalogAgent_Should partial class: feedback SPA, scripted APIs and chat clients, stuck navigation provider.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Integration")]
public partial class CatalogAgent_Should
{
  /// <summary>In-proc SPA: real catalog, WebMCP dispatcher, and store; signed-in with every permission; scripted feedback BFF.</summary>
  private sealed class FeedbackSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedFeedbackApiService Api { get; } = new();

    public FeedbackSpa()
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim("sub", Guid.NewGuid().ToString()),
          .. PermissionIds.All.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
        ],
        authenticationType: "test"));

      ServiceCollection services = new();
      services.AddLogging();
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
      services.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api);
      services.AddScoped(_ => FakeItEasy.A.Fake<Microsoft.JSInterop.IJSRuntime>());
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<PageAgentRoute>();
      services.AddScoped<AgentCallOutcome>();
      services.AddScoped<WebMcpApprovalGate>();
      services.AddScoped<WebMcpDispatcher>();

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

  /// <summary>Scripted BFF: answers SubmitFeedback with a fresh id and the server's permalink shape, echoing the command.</summary>
  private sealed class ScriptedFeedbackApiService : TimeWarp.Architecture.Services.IWebServerApiService
  {
    private static readonly DateTimeOffset FiledAt = new(2026, 10, 9, 0, 0, 0, TimeSpan.Zero);

    public List<SubmitFeedback.Command> Requests { get; } = [];
    public Guid? LastId { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      if (request is SubmitFeedback.Command command)
      {
        Requests.Add(command);
        Guid id = Guid.NewGuid();
        LastId = id;
        SubmitFeedback.Response response = new
        (
          id,
          $"/Feedback/{id:D}",
          command.Kind,
          command.Title.Trim(),
          command.Body.Trim(),
          emailCopySent: false
        );
        if (response is TResponse typed)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(typed);
        }
      }

      if (request is ListMyFeedback.Query)
      {
        List<ListMyFeedback.Item> items = [];
        for (int index = 1; index <= 21; index++)
        {
          // Item rejects Guid.Empty. Index 1 is the open-id fixture (...0001) and "Filing 0".
          Guid id = Guid.Parse($"00000000-0000-0000-0000-{index:x12}");
          items.Add
          (
            new ListMyFeedback.Item
            (
              id,
              $"/Feedback/{id:D}",
              FeedbackKind.Complaint,
              $"Filing {index - 1}",
              FiledAt
            )
          );
        }

        ListMyFeedback.Response listed = new(items, emailCopyAvailable: true);
        if (listed is TResponse listedTyped)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(listedTyped);
        }
      }

      if (request is GetFeedback.Query query)
      {
        GetFeedback.Response opened = new
        (
          query.FeedbackItemId,
          $"/Feedback/{query.FeedbackItemId:D}",
          FeedbackKind.Complaint,
          "Filing 0",
          "secret body that must not reach the model",
          FiledAt
        );
        if (opened is TResponse openedTyped)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(openedTyped);
        }
      }

      throw new InvalidOperationException($"No scripted response for {request.GetType()} → {typeof(TResponse)}.");
    }
  }

  private sealed class RecordingModelContext : IWebMcpModelContext
  {
    public List<WebMcpToolDescriptor> Tools { get; } = [];

    public ValueTask<WebMcpApplyResult> ReplaceAsync
    (
      IReadOnlyList<WebMcpToolDescriptor> tools,
      CancellationToken cancellationToken
    )
    {
      Tools.Clear();
      Tools.AddRange(tools);
      return ValueTask.FromResult(new WebMcpApplyResult(Available: true, Registered: tools.Count));
    }
  }

  private sealed class ScriptedChatClient : IChatClient
  {
    private readonly string ToolName;
    private readonly Dictionary<string, object?> Arguments;
    private bool OfferedCall;

    public ScriptedChatClient()
      : this("Counter.IncrementCounter", new Dictionary<string, object?> { ["amount"] = 5 })
    {
    }

    public ScriptedChatClient(string toolName, Dictionary<string, object?> arguments)
    {
      ToolName = toolName;
      Arguments = arguments;
    }

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      if (!OfferedCall)
      {
        OfferedCall = true;
        FunctionCallContent call = new("call-scripted", ToolName, Arguments);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
      }

      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done.")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatMessage message in response.Messages)
      {
        yield return new ChatResponseUpdate(message.Role, message.Contents);
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
  }

  private sealed class CapturingChatApi : TimeWarp.Architecture.Services.IWebServerApiService
  {
    public CompleteAgentChat.Command? Command { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      Command = request.ShouldBeOfType<CompleteAgentChat.Command>();
      CompleteAgentChat.Response response = new() { Text = "ok" };
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>
      (
        response.ShouldBeOfType<TResponse>()
      );
    }
  }

  private sealed class PageContextEchoClient : IChatClient
  {
    private bool OfferedCall;

    public string? ToolResult { get; private set; }

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      _ = options;
      _ = cancellationToken;
      foreach (ChatMessage message in messages)
      {
        foreach (AIContent content in message.Contents)
        {
          if (content is FunctionResultContent result)
          {
            ToolResult = result.Result switch
            {
              string text => text,
              JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
              _ => result.Result?.ToString(),
            };
          }
        }
      }

      if (!OfferedCall)
      {
        OfferedCall = true;
        FunctionCallContent call = new
        (
          "call-page",
          PageAgentContext.ToolName,
          new Dictionary<string, object?>()
        );
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
      }

      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatMessage message in response.Messages)
      {
        yield return new ChatResponseUpdate(message.Role, message.Contents);
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
  }

  /// <summary>
  /// Answers NavigationManager with a manager that never left the base URI.
  /// Every other service, including PageAgentRoute and IStore, comes from the shell scope.
  /// </summary>
  private sealed class StuckNavigationProvider : IServiceProvider
  {
    private readonly IServiceProvider Inner;
    private readonly NavigationManager Stuck = new TestNavigationManager();

    public StuckNavigationProvider(IServiceProvider inner)
    {
      Inner = inner;
    }

    public object? GetService(Type serviceType) =>
      serviceType == typeof(NavigationManager) ? Stuck : Inner.GetService(serviceType);
  }
}
