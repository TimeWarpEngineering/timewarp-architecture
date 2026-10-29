#region Purpose
// Browser IChatClient that posts one completion to web-server and returns the model response.
#endregion

#region Design
// The key stays on web-server. This type has no secret and no model SDK. Streaming is one
// buffered response: UIAgent reads GetStreamingResponseAsync, and FunctionInvokingChatClient
// still runs tools locally between turns. A problem detail from the relay is thrown as
// InvalidOperationException whose message is ChatReadinessProbe.Describe (status, title and the
// detail we authored), never an upstream exception (task 293: a bare 401 used to read only "The
// model request failed."). Dispose is a no-op; the API service is scoped
// to the circuit and must outlive this client. CatalogAgentSession wraps it in a second
// non-disposing client before FunctionInvokingChatClient.
// Images (task 303-001): image DataContent on a user message becomes that turn's Images. A tool
// result that carries a picture (IToolResultImage) keeps only its text in ToolResult; when it
// answers a call made after the person's last message, its picture goes on one user turn placed
// right after that run of tool turns, because a tool turn cannot carry an image. Older tool
// pictures are not sent again, so each picture reaches the model in the turn it was taken.
// The request stays within MaxImagesPerRequest: images the person attached count first (newest
// kept), then the newest tool pictures that still fit. Each dropped picture is replaced by
// OmittedPictureText so the model knows one existed. AskScreenshotChatClient sits outside the
// tool loop, so the Ask attachment stays on the person's message for every loop iteration; without
// this cap an Ask screenshot plus two capture calls in one turn would exceed it.
// A message with nothing the model reads is not sent (task 447). Approval requests and responses
// stay in the Ask history so a restored card shows its decision. FunctionInvokingChatClient
// removes a pair only while its call is not InformationalOnly, and a rejection marks the call
// InformationalOnly. A rejected pair therefore reaches this client on every later turn, and an
// empty turn fails the CompleteAgentChat validator. The model still sees the rejected call and
// its result turn.
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
      problem => throw new InvalidOperationException(ChatReadinessProbe.Describe(problem))
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

  public const string ToolPictureText = "The picture the tool returned:";

  public const string OmittedPictureText = "An earlier picture was omitted.";

  private static Command ToCommand(IEnumerable<ChatMessage> messages, ChatOptions? options)
  {
    Command command = new()
    {
      Instructions = options?.Instructions,
      Messages = ToTurns([.. messages]),
      Tools = options?.Tools?.OfType<AIFunction>().Select(ToTool).ToList() ?? []
    };
    return command;
  }

  private static List<Turn> ToTurns(List<ChatMessage> history)
  {
    List<ChatMessage> messages = history.FindAll(ReachesModel);
    int lastUser = messages.FindLastIndex(static message => message.Role == ChatRole.User);
    List<Turn> turns = [];
    List<Turn> pictureTurns = [];
    List<TurnImage> pictures = [];
    for (int index = 0; index < messages.Count; index++)
    {
      ChatMessage message = messages[index];
      turns.Add(ToTurn(message));
      if (index > lastUser)
      {
        foreach (FunctionResultContent result in message.Contents.OfType<FunctionResultContent>())
        {
          if (result.Result is IToolResultImage { Image: { } image } && ToImage(image) is { } picture)
          {
            pictures.Add(picture);
          }
        }
      }

      bool runEnds = index + 1 == messages.Count || messages[index + 1].Role != ChatRole.Tool;
      if (pictures.Count > 0 && runEnds)
      {
        Turn pictureTurn = new() { Role = ChatRole.User.Value, Text = ToolPictureText, Images = [.. pictures] };
        turns.Add(pictureTurn);
        pictureTurns.Add(pictureTurn);
        pictures.Clear();
      }
    }

    int room = MaxImagesPerRequest;
    for (int index = turns.Count - 1; index >= 0; index--)
    {
      if (!pictureTurns.Contains(turns[index]))
      {
        room = KeepNewest(turns[index], room);
      }
    }

    for (int index = pictureTurns.Count - 1; index >= 0; index--)
    {
      room = KeepNewest(pictureTurns[index], room);
    }

    return turns;
  }

  /// <summary>
  /// Whether <paramref name="message"/> maps to a turn the relay accepts: text, a tool call, a tool
  /// result, or a picture the person attached. Approval requests and responses are Ask UI content.
  /// </summary>
  private static bool ReachesModel(ChatMessage message)
  {
    if (!string.IsNullOrWhiteSpace(message.Text))
    {
      return true;
    }

    foreach (AIContent content in message.Contents)
    {
      if (content is FunctionCallContent or FunctionResultContent)
      {
        return true;
      }

      if (content is DataContent data && message.Role == ChatRole.User && ToImage(data) is not null)
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>Keeps the newest images of <paramref name="turn"/> that fit in <paramref name="room"/>.</summary>
  private static int KeepNewest(Turn turn, int room)
  {
    int count = turn.Images.Count;
    if (count == 0)
    {
      return room;
    }

    int keep = Math.Min(count, room);
    if (keep < count)
    {
      turn.Images.RemoveRange(0, count - keep);
      string note = string.Join(" ", Enumerable.Repeat(OmittedPictureText, count - keep));
      bool replaceText = string.IsNullOrEmpty(turn.Text) || (keep == 0 && turn.Text == ToolPictureText);
      turn.Text = replaceText ? note : $"{turn.Text}\n{note}";
    }

    return room - keep;
  }

  private static TurnImage? ToImage(DataContent content)
  {
    if (content.MediaType is null || !ImageMediaTypes.Contains(content.MediaType))
    {
      return null;
    }

    return new TurnImage { MediaType = content.MediaType, Data = Convert.ToBase64String(content.Data.Span) };
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
      else if (content is DataContent data && message.Role == ChatRole.User && ToImage(data) is { } image)
      {
        turn.Images.Add(image);
      }
      else if (content is FunctionResultContent result)
      {
        turn.ToolCallId = result.CallId;
        // AIFunctionFactory returns a string result as a JsonElement string. Serializing that
        // again double-encodes it ("{\u0022path\u0022:...}") for the model; send the string itself.
        turn.ToolResult = result.Result switch
        {
          string text => text,
          JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
          _ => JsonSerializer.Serialize(result.Result),
        };
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
