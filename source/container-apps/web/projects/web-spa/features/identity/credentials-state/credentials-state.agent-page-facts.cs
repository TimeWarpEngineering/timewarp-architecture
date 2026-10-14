#region Purpose
// page_context facts for pages that declare credentials actions (Settings, Passkeys).
#endregion

#region Design
// The agent revokes and renames by id and must know which rows the server allows it to touch, so
// each credential carries its id, type, nickname, label and the server's flags (CanRevoke,
// CanRename); canLinkMicrosoft365 is the server's link flag. These are the values the page's own
// buttons read. Nothing here is computed on the client.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;

partial class CredentialsState : IAgentPageFacts
{
  /// <inheritdoc />
  public void AddAgentPageFacts(JsonObject document, string path)
  {
    ArgumentNullException.ThrowIfNull(document);
    JsonArray credentials = [];
    foreach (GetCredentials.CredentialSummary credential in Credentials ?? [])
    {
      credentials.Add
      (
        new JsonObject
        {
          ["id"] = credential.Id.Value.ToString("D"),
          ["type"] = credential.Type.ToString(),
          ["nickname"] = credential.Nickname,
          ["label"] = credential.Label,
          ["isActive"] = credential.IsActive,
          ["canRevoke"] = credential.CanRevoke,
          ["canRename"] = credential.CanRename,
        }
      );
    }

    document["canLinkMicrosoft365"] = CanLinkMicrosoft365;
    document["credentials"] = credentials;
  }
}
