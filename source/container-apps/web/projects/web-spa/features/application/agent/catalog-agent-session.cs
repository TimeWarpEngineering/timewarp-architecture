#region Purpose
// Wraps the host IChatClient in the FunctionInvokingChatClient that invokes catalog tools.
#endregion

#region Design
// UIAgent forwards ChatOptions to the client and does not invoke tools itself. The client in
// front of the model is a FunctionInvokingChatClient, so tool calls and approval requests are
// handled before anything is dispatched. That client disposes the IChatClient it is given.
// The host registration is forwarded by a wrapper whose Dispose is a no-op, so disposing the
// pipeline leaves the DI IChatClient alive. The ask UI (UIAgent + FunctionApprovalBlock) is the
// only product caller. The scripted approval loop the tests drive lives in the test project, over
// this same CreateInvokingClient, so product code carries no test-only loop.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Builds the tool-invoking client over any <see cref="IChatClient"/>.</summary>
public static class CatalogAgentSession
{
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
