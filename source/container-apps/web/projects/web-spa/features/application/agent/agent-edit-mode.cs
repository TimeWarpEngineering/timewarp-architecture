#region Purpose
// The conversation's edit behavior: ask before each change, or allow edits for this conversation.
#endregion

#region Design
// Ask before editing is the default and matches CatalogAgentApproval's read-only allow-list.
// Automatically edit skips the approval wrapper and the WebMCP confirm bar for this conversation
// only. It does not change which tools are offered, and it does not grant a permission the
// principal lacks. Both drivers read the same enum so the approval bit cannot diverge.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>How a conversation treats mutating tools.</summary>
public enum AgentEditMode
{
  /// <summary>Review and approve each change. The default.</summary>
  AskBeforeEditing = 0,

  /// <summary>Allow edits for this conversation without a prompt.</summary>
  AutomaticallyEdit = 1,
}
