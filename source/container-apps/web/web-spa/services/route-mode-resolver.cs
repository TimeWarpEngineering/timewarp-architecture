namespace TimeWarp.Architecture.Services;


public static partial class RouteModeResolver
{
  [GeneratedRegex("/.*/(New|Edit|View)(/|$)")]
  private static partial Regex EditModeRegex();

  public static EditMode GetEditMode(string route)
  {
    EditMode result = EditMode.View;
    Match match = EditModeRegex().Match(route);

    if (match.Success)
    {
      string action = match.Groups[1].Value;
      if (Enum.TryParse(action, ignoreCase: true, out EditMode editMode))
      {
        result = editMode;
      }
    }
    return result;
  }
}
