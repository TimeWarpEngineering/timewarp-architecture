#region Purpose
// page_context facts for the page that declares Profile.UpdateProfile.
#endregion

#region Design
// UpdateProfile replaces the whole record, so the agent must echo the current values of every
// field it does not change. hasSnapshot is false until fetch has stored a GetProfile.Response.
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
    GetProfile.Response? profile = Profile;
    document["hasSnapshot"] = profile is not null;
    document["profile"] = new JsonObject
    {
      ["displayName"] = profile?.DisplayName,
      ["email"] = profile?.Email,
      ["language"] = profile?.Language,
      ["region"] = profile?.Region,
      ["theme"] = profile?.Theme,
      ["notifications"] = profile?.Notifications,
    };
  }
}
