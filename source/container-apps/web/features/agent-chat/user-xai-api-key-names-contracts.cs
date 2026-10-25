#region Purpose
// Name and Data Protection purpose of one user's xAI API key.
#endregion

#region Design
// The row name sits beside site.xai.api-key on the same store. The purpose includes the user id
// so user A's ciphertext cannot be unprotected with user B's purpose. dotnet new rewrites the
// TimeWarp.Architecture literal with the generated app's name, same as the site purpose. The
// length bound is the site bound: the stored suffix must not be the whole secret. The id is the
// principal Guid in "D" form, which is lowercase.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>Identity of one user's xAI API key.</summary>
public static class UserXaiApiKeyNames
{
  /// <summary>Data Protection purpose prefix. The user id is appended.</summary>
  public const string PurposePrefix = "TimeWarp.Architecture.XaiApiKey.User:";

  /// <summary>Row name for <paramref name="userId"/>.</summary>
  public static string NameFor(Guid userId) => "user." + userId.ToString("D") + ".xai.api-key";

  /// <summary>Purpose that can unprotect only <paramref name="userId"/>'s key.</summary>
  public static string PurposeFor(Guid userId) => PurposePrefix + userId.ToString("D");
}
