#region Purpose
// Browser IChatClient that posts one completion to web-server and returns the model response.
#endregion

#region Design
// The key stays on web-server. This type has no secret and no model SDK. Streaming is one
// buffered response: UIAgent reads GetStreamingResponseAsync, and FunctionInvokingChatClient
// still runs tools locally between turns. A problem detail from the relay is thrown as
// InvalidOperationException whose message is the detail we authored (setup command or a
// generic failure), never an upstream exception. Dispose is a no-op; the API service is scoped
// to the circuit and must outlive this client. CatalogAgentSession wraps it in a second
// non-disposing client before FunctionInvokingChatClient.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>Relays chat completions to the authorized web-server endpoint.</summary>
internal sealed class RelayChatClient : IChatClient
{
  private readonly IWebServerApiService Api;

  public RelayChatClient(IWebServerApiService api)
  {
    Api = api;
  }

  /// <inheritdoc />
  public async Task<ChatResponse> GetResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default
  )
  {
    Command command = ToCommand(messages, options);
    OneOf<Response, FileResponse, SharedProblemDetails> result =
      await Api.GetResponse<Response>(command, cancellationToken).ConfigureAwait(false);
    return result.Match
    (
      ToChatResponse,
      _ => throw new InvalidOperationException("The model relay returned a file."),
      problem => throw new InvalidOperationException(problem.Detail ?? "The model request failed.")
    );
  }

  /// <inheritdoc />
  public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
  (
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
  )
  {
    ChatResponse response = await GetResponseAsync(messages, options, cancellationToken)
      .ConfigureAwait(false);
    foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
    {
      yield return update;
    }
  }

  /// <inheritdoc />
  public object? GetService(Type serviceType, object? serviceKey = null)
  {
    if (serviceType == typeof(ChatClientMetadata))
    {
      return new ChatClientMetadata("xai-relay", new Uri(XaiChatDefaults.DefaultEndpoint), XaiChatDefaults.DefaultModel);
    }

    return serviceType.IsInstanceOfType(this) ? this : null;
  }

  /// <inheritdoc />
  public void Dispose()
  {
  }

  private static Command ToCommand(IEnumerable<ChatMessage> messages, ChatOptions? options)
  {
    Command command = new()
    {
      Instructions = options?.Instructions,
      Messages = messages.Select(ToTurn).ToList(),
      Tools = options?.Tools?.OfType<AIFunction>().Select(ToTool).ToList() ?? []
    };
    return command;
  }

  private static Turn ToTurn(ChatMessage message)
  {
    Turn turn = new()
    {
      Role = message.Role.Value,
      Text = string.IsNullOrEmpty(message.Text) ? null : message.Text
    };

    foreach (AIContent content in message.Contents)
    {
      if (content is FunctionCallContent call)
      {
        turn.ToolCalls.Add
        (
          new ToolCall
          {
            Name = call.Name,
            CallId = call.CallId,
            ArgumentsJson = call.Arguments is null ? "{}" : JsonSerializer.Serialize(call.Arguments)
          }
        );
      }
      else if (content is FunctionResultContent result)
      {
        turn.ToolCallId = result.CallId;
        turn.ToolResult = result.Result as string ?? JsonSerializer.Serialize(result.Result);
      }
    }

    return turn;
  }

  private static ToolDefinition ToTool(AIFunction function)
  {
    return new ToolDefinition
    {
      Name = function.Name,
      Description = function.Description,
      ParametersJson = function.JsonSchema.GetRawText()
    };
  }

  private static ChatResponse ToChatResponse(Response response)
  {
    List<AIContent> contents = [];
    if (!string.IsNullOrEmpty(response.Text))
    {
      contents.Add(new TextContent(response.Text));
    }

    foreach (ToolCall call in response.ToolCalls)
    {
      Dictionary<string, object?>? arguments = string.IsNullOrWhiteSpace(call.ArgumentsJson)
        ? null
        : JsonSerializer.Deserialize<Dictionary<string, object?>>(call.ArgumentsJson);
      contents.Add(new FunctionCallContent(call.CallId, call.Name, arguments));
    }

    if (contents.Count == 0)
    {
      contents.Add(new TextContent(string.Empty));
    }

    return new ChatResponse(new ChatMessage(ChatRole.Assistant, contents));
  }
}
