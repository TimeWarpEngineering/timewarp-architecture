#region Purpose
// page_context facts for the page that declares Profile.UpdateProfile.
#endregion

#region Design
// UpdateProfile replaces the whole record, so the agent must echo the current values of every
// field it does not change. hasSnapshot is false until the profile has loaded (Alias is null).
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using System.Text.Json.Nodes;
using TimeWarp.Architecture.Features.Applications;

partial class ProfileState : IAgentPageFacts
{
  /// <inheritdoc />
  public void AddAgentPageFacts(JsonObject document, string path)
  {
    ArgumentNullException.ThrowIfNull(document);
    document["hasSnapshot"] = Alias is not null;
    document["profile"] = new JsonObject
    {
      ["alias"] = Alias,
      ["email"] = Email,
      ["language"] = Language,
      ["region"] = Region,
      ["theme"] = Theme,
      ["notifications"] = Notifications,
    };
  }
}
