#region Purpose
// Task 292 review M1/M2: the Ask transcript survives a rebuilt agent, and the model gets the history.
#endregion

#region Design
// No host needed: UIAgent and AgentContext are plain classes. An echo IChatClient records the
// messages it is sent. One agent runs a turn on a thread; a second agent built on the same thread
// restores it through AgentContext.RestoreAsync (the panel's path) and its next request must carry
// the first prompt and answer. Generations map to threads through AskConversationThreads. An
// approval request never answered is left out of the replay; an answered one is replayed and its
// recorded decision is reported.
#endregion

namespace CatalogAgent_;

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Features.Applications;

public partial class CatalogAgent_Should
{
  public static async Task Rebuilt_Agent_Restores_Turns_And_Sends_The_History()
  {
    AskConversationThreads threads = new();
    AskConversationThread thread = threads.For(0);
    threads.For(0).ShouldBeSameAs(thread);

    using EchoChatClient first = new();
    using (UIAgent agent = new(first, options => options.Thread = thread))
    using (AgentContext context = new(agent))
    {
      await context.SendMessageAsync("first question");
      context.Status.ShouldBe(ConversationStatus.Idle);
    }

    thread.TurnCount.ShouldBe(1);

    using EchoChatClient second = new();
    using UIAgent rebuilt = new(second, options => options.Thread = threads.For(0));
    using AgentContext restored = new(rebuilt);
    await restored.RestoreAsync();
    restored.Turns.Count.ShouldBe(1);

    await restored.SendMessageAsync("second question");
    string sent = string.Join("|", second.LastMessages.Select(message => $"{message.Role}:{message.Text}"));
    sent.ShouldContain("user:first question");
    sent.ShouldContain("assistant:echo first question");
    sent.ShouldContain("user:second question");
    thread.TurnCount.ShouldBe(2);

    AskConversationThread next = threads.For(1);
    next.ShouldNotBeSameAs(thread);
    next.GetUpdates().ShouldBeEmpty();
  }

  public static Task Unanswered_Approval_Is_Not_Replayed_And_Answered_One_Reports_Its_Decision()
  {
    AskConversationThread thread = new();
    thread.AppendUserMessage(new ChatMessage(ChatRole.User, "change it"));
    thread.AppendUpdate(new ChatResponseUpdate(ChatRole.Assistant, "Let me ask."));
    thread.AppendUpdate(new ChatResponseUpdate
    {
      Role = ChatRole.Assistant,
      Contents = [new ToolApprovalRequestContent("req-1", new FunctionCallContent("call-1", "Counter.IncrementCounter"))],
    });
    thread.CompleteTurn();

    thread.ApprovalAnswer("req-1").ShouldBeNull();
    IReadOnlyList<ChatResponseUpdate> replay = thread.GetUpdates();
    replay.Count.ShouldBe(2);
    replay.SelectMany(update => update.Contents).OfType<ToolApprovalRequestContent>().ShouldBeEmpty();

    thread.AppendUserMessage(new ChatMessage(ChatRole.User, [new ToolApprovalResponseContent("req-1", approved: true, new FunctionCallContent("call-1", "Counter.IncrementCounter"))]));
    thread.AppendUpdate(new ChatResponseUpdate(ChatRole.Assistant, "Done."));
    thread.CompleteTurn();

    thread.ApprovalAnswer("req-1").ShouldBe(true);
    thread.GetUpdates().SelectMany(update => update.Contents).OfType<ToolApprovalRequestContent>().Count().ShouldBe(1);
    return Task.CompletedTask;
  }

  private sealed class EchoChatClient : IChatClient
  {
    public List<ChatMessage> LastMessages { get; } = [];

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      LastMessages.Clear();
      LastMessages.AddRange(messages);
      string prompt = LastMessages.Last(message => message.Role == ChatRole.User).Text;
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "echo " + prompt)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
      {
        yield return update;
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
  }
}
