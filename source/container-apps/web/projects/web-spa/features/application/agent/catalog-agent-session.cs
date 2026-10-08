#region Purpose
// Runs a scripted or hosted IChatClient through catalog tools, pausing for approval.
#endregion

#region Design
// UIAgent forwards ChatOptions to the client and does not invoke tools itself. The client in
// front of the model is a FunctionInvokingChatClient, so tool calls and approval requests are
// handled before anything is dispatched. That client disposes the IChatClient it is given.
// The host registration is forwarded by a wrapper whose Dispose is a no-op, so disposing the
// pipeline leaves the DI IChatClient alive. Tests drive RunAsync with a fake IChatClient.
// The ask UI uses CreateInvokingClient the same way.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>In-process catalog tool loop over any <see cref="IChatClient"/>.</summary>
public static class CatalogAgentSession
{
  private const int MaximumApprovalRounds = 8;

  public static FunctionInvokingChatClient CreateInvokingClient(IChatClient inner, IServiceProvider services)
  {
    ArgumentNullException.ThrowIfNull(inner);
    ArgumentNullException.ThrowIfNull(services);
    // FunctionInvokingChatClient owns and disposes this wrapper. The wrapper does not dispose
    // the host IChatClient, so the success path must not dispose it here either.
#pragma warning disable CA2000
    NonDisposingChatClient wrapper = new(inner);
    return new FunctionInvokingChatClient(wrapper, functionInvocationServices: services);
#pragma warning restore CA2000
  }

  public static async Task<string> RunAsync
  (
    IChatClient inner,
    IServiceProvider services,
    IList<ChatMessage> messages,
    ChatOptions options,
    Func<ToolApprovalRequestContent, bool> approve,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(messages);
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(approve);

    using FunctionInvokingChatClient client = CreateInvokingClient(inner, services);
    List<ChatMessage> history = [.. messages];
    for (int round = 0; round < MaximumApprovalRounds; round++)
    {
      int before = history.Count;
      ChatResponse response = await client.GetResponseAsync(history, options, cancellationToken);
      if (history.Count == before)
      {
        history.AddRange(response.Messages);
      }

      List<ToolApprovalRequestContent> requests = [];
      foreach (ChatMessage message in response.Messages)
      {
        foreach (AIContent content in message.Contents)
        {
          if (content is ToolApprovalRequestContent request)
          {
            requests.Add(request);
          }
        }
      }

      if (requests.Count == 0)
      {
        return response.Text ?? "";
      }

      List<AIContent> decisions = [];
      foreach (ToolApprovalRequestContent request in requests)
      {
        decisions.Add(request.CreateResponse(approve(request)));
      }

      history.Add(new ChatMessage(ChatRole.User, decisions));
    }

    throw new InvalidOperationException($"The catalog agent stopped after {MaximumApprovalRounds} approval rounds.");
  }

  private sealed class NonDisposingChatClient : IChatClient
  {
    private readonly IChatClient Inner;

    public NonDisposingChatClient(IChatClient inner)
    {
      Inner = inner;
    }

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    ) => Inner.GetResponseAsync(messages, options, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    ) => Inner.GetStreamingResponseAsync(messages, options, cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) =>
      Inner.GetService(serviceType, serviceKey);

    public void Dispose() { }
  }
}
