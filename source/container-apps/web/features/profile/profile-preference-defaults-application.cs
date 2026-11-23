#region Purpose
// The language, region, and theme a profile response carries when the stored row has none.
#endregion

#region Design
// The profile page copies the response with UpdateProfile.Command.From and does not substitute
// en-US / US / system. Create-if-missing uses these same values. A row that reached the store
// with a blank preference (a write that bypassed the domain) still has to answer the form, so
// the GET and PUT response mapping fills only null or whitespace. A real stored code is kept.
// Display name is not defaulted here: a blank name is still rejected by the response constructor.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

internal static class ProfilePreferenceDefaults
{
  public const string DisplayName = "Member";
  public const string Language = "en-US";
  public const string Region = "US";
  public const string Theme = "system";

  public static string OrLanguage(string? language) =>
    string.IsNullOrWhiteSpace(language) ? Language : language;

  public static string OrRegion(string? region) =>
    string.IsNullOrWhiteSpace(region) ? Region : region;

  public static string OrTheme(string? theme) =>
    string.IsNullOrWhiteSpace(theme) ? Theme : theme;
}
