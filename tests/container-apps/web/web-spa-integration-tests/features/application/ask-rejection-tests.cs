#region Purpose
// Task 447: rejecting an Ask approval ends that turn with an answer, and later turns and Retry keep working.
#endregion

#region Design
// The product Ask path with no host: UIAgent and AgentContext over CatalogAgentSession
// .CreateInvokingClient over RelayChatClient, writing to an AskConversationThread, as AgentAsk
// builds it. The tools are the real catalog selection on an in-proc SPA (CatalogAgent_Should
// .CaptureSpa), so Screenshot.CaptureScreenshot is the product's approval-required tool. The
// person's Reject is FunctionApprovalBlock.Reject("Declined."), called when the block appears.
// The web-server stand-in runs CompleteAgentChat.Validator on every command and answers a failure
// with the 400 problem the real endpoint returns, so a malformed relay request puts the context in
// Error the way the panel saw it. Its scripted model asks for a capture on "Filter bar" and
// otherwise answers in text. FailNext fails one request so Retry can be proved.
#endregion

namespace AskRejection_;

using System.Security.Claims;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Foundation.Features;
using CatalogAgent_;

[TestTag("Unit")]
public class AskRejection_Should
{
  private const string CaptureTool = "Screenshot.CaptureScreenshot";
  private const string CaptureCallId = "call-capture";
  private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskRejection_Should>();

  public static async Task Rejected_Capture_Answers_And_Later_Turns_And_Retry_Do_Not_Error()
  {
    CatalogAgent_Should.CaptureJsRuntime browser = new(new ScreenshotJsModule.BrowserCapture { Token = "shot-1" });
    using CatalogAgent_Should.CaptureSpa spa = new(browser);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Settings");
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(await SelectAsync(spa.User, scope));
    functions.Tools.Single(tool => tool.Name == CaptureTool).ShouldBeOfType<ApprovalRequiredAIFunction>();
    ChatOptions options = new() { Tools = [.. functions.Tools] };
    ScriptedModelApi api = new();
    AskConversationThread thread = new AskConversationThreads().For(0);
    string requestId;

    using (FunctionInvokingChatClient client = CatalogAgentSession.CreateInvokingClient(new RelayChatClient(api), scope.ServiceProvider))
    using (UIAgent agent = new(client, agentOptions => Configure(agentOptions, options, thread)))
    using (AgentContext context = new(agent))
    {
      List<FunctionApprovalBlock> approvals = [];
      using IDisposable registration = context.RegisterOnBlockAdded
      (
        (_, block) =>
        {
          if (block is FunctionApprovalBlock approval)
          {
            approvals.Add(approval);
            approval.Reject("Declined.");
          }
        }
      );

      await context.SendMessageAsync("show me the Filter bar").WaitAsync(Timeout);
      ShouldBeIdle(context, api);
      FunctionApprovalBlock rejected = approvals.ShouldHaveSingleItem();
      requestId = rejected.ApprovalRequest.RequestId;
      browser.Selector.ShouldBeNull("a rejected capture never reaches the browser");
      CompleteAgentChat.Turn decline = api.Commands[^1].Messages.Single(turn => turn.ToolCallId == CaptureCallId);
      decline.ToolResult.ShouldNotBeNull().ShouldContain("rejected");
      decline.ToolResult.ShouldContain("Declined.");
      api.Replies[^1].ShouldBe(ScriptedModelApi.DeclinedReply);
      thread.ApprovalAnswer(requestId).ShouldBe(false, "the thread records the rejection");

      await context.SendMessageAsync("why didn't you find the route?").WaitAsync(Timeout);
      ShouldBeIdle(context, api);
      api.Replies[^1].ShouldBe("Answered: why didn't you find the route?");

      // A failed turn is retried with the history that failed; the retry must go through.
      api.FailNext = true;
      await context.SendMessageAsync("and the Filter bar route?").WaitAsync(Timeout);
      context.Status.ShouldBe(ConversationStatus.Error);
      await context.RetryAsync().WaitAsync(Timeout);
      ShouldBeIdle(context, api);
      api.Replies[^1].ShouldBe("Answered: and the Filter bar route?");
    }

    // The panel closes and reopens: a rebuilt agent restores the thread and keeps talking.
    using FunctionInvokingChatClient rebuiltClient = CatalogAgentSession.CreateInvokingClient(new RelayChatClient(api), scope.ServiceProvider);
    using UIAgent rebuilt = new(rebuiltClient, agentOptions => Configure(agentOptions, options, thread));
    using AgentContext restored = new(rebuilt);
    await restored.RestoreAsync().WaitAsync(Timeout);
    thread.ApprovalAnswer(requestId).ShouldBe(false, "the restored card shows Rejected");
    await restored.SendMessageAsync("one more question").WaitAsync(Timeout);
    ShouldBeIdle(restored, api);
    api.Replies[^1].ShouldBe("Answered: one more question");
    browser.Selector.ShouldBeNull();
  }

  public static async Task Relay_Leaves_Answered_Approvals_Out_Of_The_Command()
  {
    ScriptedModelApi api = new();
    using RelayChatClient client = new(api);
    FunctionCallContent call = new(CaptureCallId, CaptureTool, new Dictionary<string, object?>());
    ToolApprovalRequestContent request = new("req-1", call);
    ChatMessage[] history =
    [
      new(ChatRole.User, "show me the Filter bar"),
      new(ChatRole.Assistant, [request]),
      new(ChatRole.User, [request.CreateResponse(approved: false, "Declined.")]),
      new(ChatRole.Assistant, [call]),
      new(ChatRole.Tool, [new FunctionResultContent(CaptureCallId, "Tool call invocation was rejected.")]),
      new(ChatRole.Assistant, "You declined."),
      new(ChatRole.User, "why?"),
    ];

    await client.GetResponseAsync(history);

    api.Failures.ShouldBeEmpty();
    string.Join("|", api.Commands.Single().Messages.Select(static turn => turn.Role))
      .ShouldBe("user|assistant|tool|assistant|user");
  }

  private static void Configure(UIAgentOptions agentOptions, ChatOptions options, AskConversationThread thread)
  {
    agentOptions.ChatOptions = options;
    agentOptions.Thread = thread;
  }

  private static void ShouldBeIdle(AgentContext context, ScriptedModelApi api)
  {
    context.Error.ShouldBeNull(context.Error?.Message);
    context.Status.ShouldBe(ConversationStatus.Idle);
    api.Failures.ShouldBeEmpty();
  }

  private static Task<IReadOnlyList<CatalogAgentTool>> SelectAsync(ClaimsPrincipal user, SpaTestScope scope) =>
    CatalogAgentToolSet.SelectAsync
    (
      user,
      scope.ServiceProvider.GetRequiredService<IAuthorizationService>(),
      scope.ServiceProvider.GetRequiredService<IActionCatalog>().Entries,
      "/Settings",
      CancellationToken.None
    );

  /// <summary>web-server's completion endpoint: validates like the real one, then scripts the model.</summary>
  private sealed class ScriptedModelApi : IWebServerApiService
  {
    public const string DeclinedReply = "You declined the screenshot, so I did not look.";

    public List<CompleteAgentChat.Command> Commands { get; } = [];

    public List<string> Failures { get; } = [];

    public List<string> Replies { get; } = [];

    public bool FailNext { get; set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      CompleteAgentChat.Command command = request.ShouldBeOfType<CompleteAgentChat.Command>();
      Commands.Add(command);
      ValidationResult validation = new CompleteAgentChat.Validator().Validate(command);
      if (!validation.IsValid)
      {
        string detail = string.Join(" ", validation.Errors.Select(static error => error.ErrorMessage));
        Failures.Add(detail);
        return Problem<TResponse>(400, "One or more validation errors occurred.", detail);
      }

      if (FailNext)
      {
        FailNext = false;
        return Problem<TResponse>(502, "The model request failed.", "Scripted transient failure.");
      }

      CompleteAgentChat.Response response = Answer(command);
      Replies.Add(response.Text ?? "");
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response.ShouldBeOfType<TResponse>());
    }

    private static Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> Problem<TResponse>
    (
      int status,
      string title,
      string detail
    ) where TResponse : class =>
      Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>
      (
        new SharedProblemDetails { Status = status, Title = title, Detail = detail }
      );

    private static CompleteAgentChat.Response Answer(CompleteAgentChat.Command command)
    {
      int lastUser = command.Messages.FindLastIndex(static turn => turn.Role == "user");
      CompleteAgentChat.Turn user = command.Messages[lastUser];
      if (command.Messages.Skip(lastUser + 1).Any(static turn => turn.Role == "tool"))
      {
        return new CompleteAgentChat.Response { Text = DeclinedReply };
      }

      if (user.Text == "show me the Filter bar")
      {
        CompleteAgentChat.ToolCall capture = new() { Name = CaptureTool, CallId = CaptureCallId, ArgumentsJson = "{}" };
        return new CompleteAgentChat.Response { ToolCalls = [capture] };
      }

      return new CompleteAgentChat.Response { Text = $"Answered: {user.Text}" };
    }
  }
}
