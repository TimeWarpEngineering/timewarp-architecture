#region Purpose
// Chat-client and HTTP-response doubles for the GenAI telemetry tests.
#endregion

#region Design
// Scripted clients return a fixed reply with token usage and never dial xAI.
// ScriptedPipelineResponse carries a status and a body. Its Authorization header is present so a
// test can prove the log path never reads headers.
#endregion

namespace XaiChatTelemetry_;

using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Features.AgentChats;

internal sealed class RecordingFactory : IXaiChatClientFactory
{
  public List<bool> Flags { get; } = [];

  public IChatClient Create(string apiKey, string endpoint, string model, bool enableSensitiveTelemetry)
  {
    _ = apiKey;
    _ = endpoint;
    _ = model;
    Flags.Add(enableSensitiveTelemetry);
    return new ScriptedChatClient("ok");
  }
}

internal sealed class ScriptedChatClient : IChatClient
{
  private readonly string Reply;

  public ScriptedChatClient(string reply) => Reply = reply;

  public Task<ChatResponse> GetResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)
  {
    _ = messages;
    _ = options;
    _ = cancellationToken;
    ChatResponse response = new(new ChatMessage(ChatRole.Assistant, Reply));
    response.ModelId = "grok-4.7";
    response.Usage = new UsageDetails
    {
      InputTokenCount = 3,
      OutputTokenCount = 5,
      TotalTokenCount = 8
    };
    return Task.FromResult(response);
  }

  public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public object? GetService(Type serviceType, object? serviceKey = null) => null;

  public void Dispose()
  {
  }
}

internal sealed class ThrowingChatClient : IChatClient
{
  private readonly ClientResultException Exception;

  public ThrowingChatClient(ClientResultException exception) => Exception = exception;

  public Task<ChatResponse> GetResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)
  {
    _ = messages;
    _ = options;
    _ = cancellationToken;
    return Task.FromException<ChatResponse>(Exception);
  }

  public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default) =>
    throw new NotSupportedException();

  public object? GetService(Type serviceType, object? serviceKey = null) => null;

  public void Dispose()
  {
  }
}

internal sealed class ScriptedPipelineResponse : PipelineResponse
{
  public ScriptedPipelineResponse(int status, string reasonPhrase, string body, string authorization)
  {
    Status = status;
    ReasonPhrase = reasonPhrase;
    Content = BinaryData.FromString(body);
    HeadersCore = new ScriptedHeaders(authorization);
  }

  public override int Status { get; }

  public override string ReasonPhrase { get; }

  public override BinaryData Content { get; }

  public override Stream? ContentStream { get; set; }

  protected override PipelineResponseHeaders HeadersCore { get; }

  public override BinaryData BufferContent(CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return Content;
  }

  public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return ValueTask.FromResult(Content);
  }

  public override void Dispose()
  {
  }

  private sealed class ScriptedHeaders : PipelineResponseHeaders
  {
    private readonly string Authorization;

    public ScriptedHeaders(string authorization) => Authorization = authorization;

    public override bool TryGetValue(string name, out string? value)
    {
      if (string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase))
      {
        value = Authorization;
        return true;
      }

      value = null;
      return false;
    }

    public override bool TryGetValues(string name, out IEnumerable<string>? values)
    {
      if (TryGetValue(name, out string? value) && value is not null)
      {
        values = [value];
        return true;
      }

      values = null;
      return false;
    }

    public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
      yield return new KeyValuePair<string, string>("Authorization", Authorization);
    }
  }
}
