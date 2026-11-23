#region Purpose
// Compares and copies the profile page's working command without touching the store.
#endregion

#region Design
// The working copy stays on the page (task 489). A baseline copy is how the page knows the
// person has typed something since the last load, cancel, or save. Apply does not refresh the
// baseline: a draft stays unsaved until the person saves. UserId is not part of the form.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

/// <summary>Field copy and equality for the profile edit form's working command.</summary>
public static class ProfileFormDraft
{
  public static UpdateProfile.Command Copy(UpdateProfile.Command source)
  {
    ArgumentNullException.ThrowIfNull(source);
    return new UpdateProfile.Command
    {
      DisplayName = source.DisplayName,
      Email = source.Email,
      Language = source.Language,
      Region = source.Region,
      Theme = source.Theme,
      Notifications = source.Notifications,
    };
  }

  public static bool Same(UpdateProfile.Command left, UpdateProfile.Command right)
  {
    ArgumentNullException.ThrowIfNull(left);
    ArgumentNullException.ThrowIfNull(right);
    return left.DisplayName == right.DisplayName
      && left.Email == right.Email
      && left.Language == right.Language
      && left.Region == right.Region
      && left.Theme == right.Theme
      && left.Notifications == right.Notifications;
  }
}
