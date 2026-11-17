#region Purpose
// Maps the Ask relay command and response onto Microsoft.Extensions.AI chat types.
#endregion

#region Design
// Tools are AIFunctionDeclaration instances. Invoke on a declaration throws, so this process
// cannot run a catalog action on the model's behalf. Relay images map to DataContent, which the
// OpenAI adapter sends as image content. A tool schema or argument payload that is not JSON
// throws JsonException; XaiChatUpstream turns that into a 400.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Text.Json;
using Microsoft.Extensions.AI;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

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

      foreach (TurnImage image in turn.Images)
      {
        contents.Add(new DataContent(Convert.FromBase64String(image.Data), image.MediaType));
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
