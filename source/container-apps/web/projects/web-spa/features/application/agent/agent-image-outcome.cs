#region Purpose
// Lets an agent outcome carry a picture that reaches the in-app model as image content.
#endregion

#region Design
// Task 303-001. WebMCP serializes AgentCallOutcome.Result whole, so a capture's data: URI is in
// the JSON the browser agent gets. The in-app model must not get it as tool-result text: the relay
// caps a tool result at CompleteAgentChat.MaxMessageTextLength. CatalogAgentFunctions therefore
// returns WithoutImage() as the result and puts ToImage() on the call result (IToolResultImage),
// and RelayChatClient sends that picture as an image on a user turn right after the tool turns.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>An AgentCallOutcome result that holds a picture.</summary>
public interface IAgentImageOutcome
{
  /// <summary>The same outcome without the picture, for tool-result text.</summary>
  object WithoutImage();

  /// <summary>The picture as model image content.</summary>
  DataContent ToImage();
}

/// <summary>A tool result whose picture travels beside the result text.</summary>
public interface IToolResultImage
{
  DataContent? Image { get; }
}
