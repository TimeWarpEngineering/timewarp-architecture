#region Purpose
// Forwards one completion to xAI, or to the in-process fake when tests ask for it.
#endregion

#region Design
// Chat completions only (GetChatClient.AsIChatClient). The Responses API on adapter 10.10.0
// TypeLoadExceptions when a tool call meets OpenAI 2.14. The real client is created on first
// use so startup never dials xAI and a PostConfigure in tests wins over the key.
// UseFakeUpstream is honored only in Development or Testing, and then it wins over a real key.
// Tools are AIFunctionDeclaration instances. Invoke on a declaration throws, so this process
// cannot run a catalog action on the model's behalf. Failures are logged and replaced with
// AgentChatProblems.UpstreamFailed so the client never sees exception text.
// The fake asks for page_context, the read-only tool PageAgentContext registers. A later turn
// that already contains a tool result is answered with text that includes that result.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using TimeWarp.Architecture.Features.AgentChats.Application;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>xAI chat completions, or the deterministic fake.</summary>
internal sealed class XaiChatUpstream : IAgentChatUpstream, IDisposable
{
  private static readonly Action<ILogger, Exception?> LogUpstreamFailed =
    LoggerMessage.Define
    (
      LogLevel.Error,
      new EventId(1, nameof(LogUpstreamFailed)),
      "xAI chat completion failed"
    );

  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly IHostEnvironment Environment;
  private readonly ILogger<XaiChatUpstream> Logger;
  private readonly Lock Gate = new();
  private readonly FakeChatClient Fake = new();
  private OpenAIClient? OpenAiClient;
  private IChatClient? RealClient;

  public XaiChatUpstream
  (
    IOptionsMonitor<XaiChatOptions> options,
    IHostEnvironment environment,
    ILogger<XaiChatUpstream> logger
  )
  {
    Options = options;
    Environment = environment;
    Logger = logger;
  }

  private bool AllowFake =>
    Environment.IsDevelopment() || Environment.IsEnvironment("Testing");

  /// <inheritdoc />
  public bool IsConfigured
  {
    get
    {
      XaiChatOptions options = Options.CurrentValue;
      if (AllowFake && options.UseFakeUpstream)
      {
        return true;
      }

      return !string.IsNullOrWhiteSpace(options.ApiKey);
    }
  }

  /// <inheritdoc />
  public string? Model
  {
    get
    {
      if (!IsConfigured)
      {
        return null;
      }

      string model = Options.CurrentValue.Model;
      return string.IsNullOrWhiteSpace(model) ? XaiChatDefaults.DefaultModel : model;
    }
  }

  /// <inheritdoc />
  public async Task<OneOf<Response, SharedProblemDetails>> CompleteAsync
  (
    Command command,
    CancellationToken cancellationToken
  )
  {
    XaiChatOptions options = Options.CurrentValue;
    IChatClient? client;
    try
    {
      client = GetClient(options);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      LogUpstreamFailed(Logger, exception);
      return AgentChatProblems.UpstreamFailed();
    }

    if (client is null)
    {
      return AgentChatProblems.NotConfigured();
    }

    ChatOptions chatOptions;
    List<ChatMessage> messages;
    try
    {
      chatOptions = XaiChatMapping.ToChatOptions(command);
      messages = XaiChatMapping.ToMessages(command);
    }
    catch (JsonException exception)
    {
      LogUpstreamFailed(Logger, exception);
      return new SharedProblemDetails
      {
        Title = "Invalid tool schema",
        Status = 400,
        Detail = "A tool schema or argument payload was not valid JSON."
      };
    }

    ChatResponse chatResponse;
    try
    {
      chatResponse = await client.GetResponseAsync(messages, chatOptions, cancellationToken)
        .ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      LogUpstreamFailed(Logger, exception);
      return AgentChatProblems.UpstreamFailed();
    }

    var mapped = XaiChatMapping.ToResponse(chatResponse);
    if (string.IsNullOrWhiteSpace(mapped.Text) && mapped.ToolCalls.Count == 0)
    {
      return AgentChatProblems.EmptyModelResponse();
    }

    return mapped;
  }

  /// <summary>Disposes the real client, if one was created.</summary>
  public void Dispose()
  {
    // OpenAIClient has no Dispose. AsIChatClient owns the pipeline it needs.
    RealClient?.Dispose();
    Fake.Dispose();
  }

  private IChatClient? GetClient(XaiChatOptions options)
  {
    if (AllowFake && options.UseFakeUpstream)
    {
      return Fake;
    }

    if (string.IsNullOrWhiteSpace(options.ApiKey))
    {
      return null;
    }

    lock (Gate)
    {
      if (RealClient is not null)
      {
        return RealClient;
      }

      string endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
        ? XaiChatDefaults.DefaultEndpoint
        : options.Endpoint;
      string model = string.IsNullOrWhiteSpace(options.Model)
        ? XaiChatDefaults.DefaultModel
        : options.Model;
      OpenAiClient = new OpenAIClient
      (
        new ApiKeyCredential(options.ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) }
      );
      RealClient = OpenAiClient.GetChatClient(model).AsIChatClient();
      return RealClient;
    }
  }

  private sealed class FakeChatClient : IChatClient
  {
    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      FunctionResultContent? result = messages
        .SelectMany(message => message.Contents)
        .OfType<FunctionResultContent>()
        .LastOrDefault();
      if (result is not null)
      {
        string rendered = result.Result as string ?? JsonSerializer.Serialize(result.Result);
        ChatMessage answer = new(ChatRole.Assistant, $"page_context: {rendered}");
        return Task.FromResult(new ChatResponse(answer));
      }

      FunctionCallContent call = new
      (
        "call-1",
        "page_context",
        new Dictionary<string, object?>()
      );
      ChatMessage toolTurn = new(ChatRole.Assistant, [call]);
      return Task.FromResult(new ChatResponse(toolTurn));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken)
        .ConfigureAwait(false);
      foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
      {
        yield return update;
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
      serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
    }
  }
}

/// <summary>Maps relay DTOs onto Microsoft.Extensions.AI types. Tools stay declaration-only.</summary>
internal static class XaiChatMapping
{
  public static List<ChatMessage> ToMessages(Command command)
  {
    List<ChatMessage> messages = [];
    foreach (Turn turn in command.Messages)
    {
      ChatRole role = turn.Role.ToLowerInvariant() switch
      {
        "system" => ChatRole.System,
        "user" => ChatRole.User,
        "assistant" => ChatRole.Assistant,
        "tool" => ChatRole.Tool,
        _ => throw new InvalidOperationException("Role was not validated.")
      };

      List<AIContent> contents = [];
      if (!string.IsNullOrEmpty(turn.Text))
      {
        contents.Add(new TextContent(turn.Text));
      }

      foreach (ToolCall call in turn.ToolCalls)
      {
        contents.Add(new FunctionCallContent(call.CallId, call.Name, ParseArguments(call.ArgumentsJson)));
      }

      if (!string.IsNullOrWhiteSpace(turn.ToolCallId))
      {
        contents.Add(new FunctionResultContent(turn.ToolCallId, turn.ToolResult ?? ""));
      }

      if (contents.Count == 0)
      {
        contents.Add(new TextContent(string.Empty));
      }

      messages.Add(new ChatMessage(role, contents));
    }

    return messages;
  }

  public static ChatOptions ToChatOptions(Command command)
  {
    ChatOptions options = new();
    if (!string.IsNullOrWhiteSpace(command.Instructions))
    {
      options.Instructions = command.Instructions;
    }

    if (command.Tools.Count > 0)
    {
      options.Tools = ToTools(command.Tools);
    }

    return options;
  }

  public static List<AITool> ToTools(IReadOnlyList<ToolDefinition> tools)
  {
    List<AITool> mapped = [];
    foreach (ToolDefinition tool in tools)
    {
      using var document = JsonDocument.Parse(tool.ParametersJson);
      mapped.Add
      (
        AIFunctionFactory.CreateDeclaration
        (
          tool.Name,
          tool.Description ?? "",
          document.RootElement.Clone()
        )
      );
    }

    return mapped;
  }

  public static Response ToResponse(ChatResponse chatResponse)
  {
    Response response = new();
    foreach (ChatMessage message in chatResponse.Messages)
    {
      foreach (AIContent content in message.Contents)
      {
        if (content is TextContent text && !string.IsNullOrEmpty(text.Text))
        {
          response.Text = string.IsNullOrEmpty(response.Text) ? text.Text : response.Text + text.Text;
        }
        else if (content is FunctionCallContent call)
        {
          response.ToolCalls.Add
          (
            new ToolCall
            {
              Name = call.Name,
              CallId = call.CallId,
              ArgumentsJson = call.Arguments is null
                ? "{}"
                : JsonSerializer.Serialize(call.Arguments)
            }
          );
        }
      }
    }

    if (string.IsNullOrEmpty(response.Text))
    {
      response.Text = string.IsNullOrEmpty(chatResponse.Text) ? null : chatResponse.Text;
    }

    return response;
  }

  private static Dictionary<string, object?>? ParseArguments(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    return JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
  }
}
