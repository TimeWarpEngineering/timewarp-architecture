#region Purpose
// AssemblyInfoText: the version dialog's heading, rows, and links, and the plain text its Copy
// button writes.
#endregion

#region Design
// Everything the dialog shows comes from here, and ClipboardText is built from the same three
// members, so the clipboard matches the screen. Title is the Section heading, Properties are
// the PropertyDisplay rows, and Links are the LinkDisplay rows. Blank values are dropped here
// with the same IsNullOrWhiteSpace test PropertyDisplay and LinkDisplay apply. ClipboardText is
// one "Name: value" line per row: Title first, then the property rows in display order, then
// each link's URL under its Name. Lines join with "\n", like NotificationState.ClipboardText.
#endregion

namespace TimeWarp.Architecture.Components;

public static class AssemblyInfoText
{
  /// <summary>The dialog's Section heading; null when the record is not loaded or has no title.</summary>
  public static string? Title(AssemblyInfo? assemblyInfo) =>
    string.IsNullOrWhiteSpace(assemblyInfo?.Title) ? null : assemblyInfo.Title;

  /// <summary>The dialog's property rows, in display order, with blank values omitted.</summary>
  public static IReadOnlyList<(string Name, string Value)> Properties(AssemblyInfo assemblyInfo)
  {
    ArgumentNullException.ThrowIfNull(assemblyInfo);
    return NonBlank
    (
      (nameof(AssemblyInfo.AlgorithmId), assemblyInfo.AlgorithmId?.ToString(CultureInfo.InvariantCulture)),
      (nameof(AssemblyInfo.Description), assemblyInfo.Description),
      (nameof(AssemblyInfo.Configuration), assemblyInfo.Configuration),
      (nameof(AssemblyInfo.Company), assemblyInfo.Company),
      (nameof(AssemblyInfo.Product), assemblyInfo.Product),
      (nameof(AssemblyInfo.Copyright), assemblyInfo.Copyright),
      (nameof(AssemblyInfo.Trademark), assemblyInfo.Trademark),
      (nameof(AssemblyInfo.Culture), assemblyInfo.Culture),
      (nameof(AssemblyInfo.Version), assemblyInfo.Version),
      (nameof(AssemblyInfo.FileVersion), assemblyInfo.FileVersion),
      (nameof(AssemblyInfo.InformationalVersion), assemblyInfo.InformationalVersion),
      (nameof(AssemblyInfo.NeutralResourcesLanguage), assemblyInfo.NeutralResourcesLanguage),
      (nameof(AssemblyInfo.ClsCompliant), assemblyInfo.ClsCompliant),
      (nameof(AssemblyInfo.InternalsVisibleTo), assemblyInfo.InternalsVisibleTo),
      (nameof(AssemblyInfo.DelaySign), assemblyInfo.DelaySign),
      (nameof(AssemblyInfo.KeyFile), assemblyInfo.KeyFile),
      (nameof(AssemblyInfo.KeyName), assemblyInfo.KeyName),
      (nameof(AssemblyInfo.CommitHash), assemblyInfo.CommitHash),
      (nameof(AssemblyInfo.ShortHash), assemblyInfo.ShortHash),
      (nameof(AssemblyInfo.CommitDate), assemblyInfo.CommitDate?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
    );
  }

  /// <summary>The dialog's LinkDisplay rows, in display order, with blank URLs omitted.</summary>
  public static IReadOnlyList<(string Name, string Url, string DisplayText)> Links(AssemblyInfo assemblyInfo)
  {
    ArgumentNullException.ThrowIfNull(assemblyInfo);
    (string Name, string? Url, string DisplayText)[] links =
    [
      (nameof(AssemblyInfo.CommitUrl), assemblyInfo.CommitUrl, "View Commit Details"),
      (nameof(AssemblyInfo.RepositoryUrl), assemblyInfo.RepositoryUrl, "View Repository"),
    ];
    return [.. links.Where(link => !string.IsNullOrWhiteSpace(link.Url)).Select(link => (link.Name, link.Url!, link.DisplayText))];
  }

  /// <summary>Everything the dialog shows as "Name: value" lines, blank values omitted.</summary>
  public static string ClipboardText(AssemblyInfo assemblyInfo)
  {
    ArgumentNullException.ThrowIfNull(assemblyInfo);
    IEnumerable<(string Name, string Value)> rows =
      NonBlank((nameof(AssemblyInfo.Title), Title(assemblyInfo)))
        .Concat(Properties(assemblyInfo))
        .Concat(Links(assemblyInfo).Select(link => (link.Name, link.Url)));
    return string.Join("\n", rows.Select(row => $"{row.Name}: {row.Value}"));
  }

  private static List<(string Name, string Value)> NonBlank(params (string Name, string? Value)[] rows) =>
    [.. rows.Where(row => !string.IsNullOrWhiteSpace(row.Value)).Select(row => (row.Name, row.Value!))];
}
