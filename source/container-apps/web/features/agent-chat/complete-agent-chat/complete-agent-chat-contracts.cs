#region Purpose
// One chat-completion turn for the browser agent. The server forwards it and does not run tools.
#endregion

#region Design
// The browser FunctionInvokingChatClient owns the tool loop. This command carries the transcript,
// the instruction string, and tool declarations (name, description, JSON schema). The response
// carries assistant text and any function calls. An empty text plus an empty tool-call list is
// not a valid model reply; the handler turns that into a problem.
// Limits are the anti-proxy budget: message count, per-field lengths, and a summed character cap.
// Roles are the four chat roles. A tool turn must name the call it answers.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = XaiChatDefaults.AuthenticatedPolicy,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class CompleteAgentChat
{
  public const int MaxMessages = 40;
  public const int MaxMessageTextLength = 16_000;
  public const int MaxTools = 32;
  public const int MaxToolNameLength = 128;
  public const int MaxToolDescriptionLength = 2_000;
  public const int MaxParametersJsonLength = 8_000;
  public const int MaxInstructionsLength = 8_000;
  public const int MaxToolCallsPerMessage = 8;
  public const int MaxToolArgumentsLength = 8_000;
  public const int MaxRequestCharacters = 256_000;
  public const int MaxConcurrentPerPrincipal = 2;
  public const int MaxRequestsPerMinute = 30;

  [ApiRoute("api/agent-chat/completions", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public List<Turn> Messages { get; set; } = [];

    public string? Instructions { get; set; }

    public List<ToolDefinition> Tools { get; set; } = [];
  }

  public sealed class Turn
  {
    public string Role { get; set; } = null!;

    public string? Text { get; set; }

    public string? ToolCallId { get; set; }

    public string? ToolResult { get; set; }

    public List<ToolCall> ToolCalls { get; set; } = [];
  }

  public sealed class ToolCall
  {
    public string Name { get; set; } = null!;

    public string CallId { get; set; } = null!;

    public string ArgumentsJson { get; set; } = "{}";
  }

  public sealed class ToolDefinition
  {
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string ParametersJson { get; set; } = """{"type":"object","properties":{}}""";
  }

  public sealed class Response : BaseResponse
  {
    public string? Text { get; set; }

    public List<ToolCall> ToolCalls { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    private static readonly HashSet<string> Roles =
    [
      "system", "user", "assistant", "tool"
    ];

    public Validator()
    {
      RuleFor(command => command.Messages)
        .Cascade(CascadeMode.Stop)
        .NotNull()
        .Must(messages => messages.Count is > 0 and <= MaxMessages)
        .WithMessage($"Messages must contain between 1 and {MaxMessages} turns.");

      RuleFor(command => command.Instructions)
        .MaximumLength(MaxInstructionsLength);

      RuleFor(command => command.Tools)
        .Cascade(CascadeMode.Stop)
        .NotNull()
        .Must(tools => tools.Count <= MaxTools)
        .WithMessage($"Tools must contain at most {MaxTools} declarations.");

      RuleForEach(command => command.Tools).ChildRules(tool =>
      {
        tool.RuleFor(item => item.Name).NotEmpty().MaximumLength(MaxToolNameLength);
        tool.RuleFor(item => item.Description).MaximumLength(MaxToolDescriptionLength);
        tool.RuleFor(item => item.ParametersJson).NotEmpty().MaximumLength(MaxParametersJsonLength);
      });

      RuleForEach(command => command.Messages).ChildRules(turn =>
      {
        turn.RuleFor(item => item.Role)
          .Must(role => role is not null && Roles.Contains(role))
          .WithMessage("Role must be system, user, assistant, or tool.");
        turn.RuleFor(item => item.Text).MaximumLength(MaxMessageTextLength);
        turn.RuleFor(item => item.ToolResult).MaximumLength(MaxMessageTextLength);
        turn.RuleFor(item => item.ToolCalls)
          .Must(calls => calls?.Count <= MaxToolCallsPerMessage);
        turn.RuleFor(item => item)
          .Must(item => !IsEmpty(item))
          .WithMessage("A turn needs text, a tool call, or a tool result.");
        turn.RuleFor(item => item)
          .Must(item => item.Role != "tool" || !string.IsNullOrWhiteSpace(item.ToolCallId))
          .WithMessage("A tool turn needs ToolCallId.");
        turn.RuleForEach(item => item.ToolCalls).ChildRules(call =>
        {
          call.RuleFor(item => item.Name).NotEmpty().MaximumLength(MaxToolNameLength);
          call.RuleFor(item => item.CallId).NotEmpty().MaximumLength(MaxToolNameLength);
          call.RuleFor(item => item.ArgumentsJson).MaximumLength(MaxToolArgumentsLength);
        });
      });

      RuleFor(command => command)
        .Must(FitsBudget)
        .WithMessage($"The request exceeds {MaxRequestCharacters} characters.");
    }

    private static bool IsEmpty(Turn turn)
    {
      bool hasText = !string.IsNullOrWhiteSpace(turn.Text);
      bool hasCalls = turn.ToolCalls is { Count: > 0 };
      bool hasResult = !string.IsNullOrWhiteSpace(turn.ToolResult) || !string.IsNullOrWhiteSpace(turn.ToolCallId);
      return !hasText && !hasCalls && !hasResult;
    }

    private static bool FitsBudget(Command command)
    {
      int size = command.Instructions?.Length ?? 0;
      if (command.Tools is not null)
      {
        foreach (ToolDefinition tool in command.Tools)
        {
          size += tool.Name?.Length ?? 0;
          size += tool.Description?.Length ?? 0;
          size += tool.ParametersJson?.Length ?? 0;
        }
      }

      if (command.Messages is not null)
      {
        foreach (Turn turn in command.Messages)
        {
          size += turn.Role?.Length ?? 0;
          size += turn.Text?.Length ?? 0;
          size += turn.ToolCallId?.Length ?? 0;
          size += turn.ToolResult?.Length ?? 0;
          if (turn.ToolCalls is null)
          {
            continue;
          }

          foreach (ToolCall call in turn.ToolCalls)
          {
            size += call.Name?.Length ?? 0;
            size += call.CallId?.Length ?? 0;
            size += call.ArgumentsJson?.Length ?? 0;
          }
        }
      }

      return size <= MaxRequestCharacters;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response { Text = "AI not configured", ToolCalls = [] };
}
