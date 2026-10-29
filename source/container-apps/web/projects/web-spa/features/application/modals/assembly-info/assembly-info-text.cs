#region Purpose
// AssemblyInfoText: the version dialog's property rows and the plain text its Copy button writes.
#endregion

#region Design
// One list feeds both the dialog's PropertyDisplay rows and ClipboardText, so the clipboard
// matches the screen. Blank values are dropped here with the same IsNullOrWhiteSpace test
// PropertyDisplay applies. ClipboardText is one "Name: value" line per row: Title (the section
// heading) first, then the property rows in display order, then the two LinkDisplay URLs as
// CommitUrl and RepositoryUrl. Lines join with "\n", like NotificationState.ClipboardText.
#endregion

namespace TimeWarp.Architecture.Components;

using TimeWarp.State.Plus.Extensions;

public static class AssemblyInfoText
{
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

  /// <summary>Everything the dialog shows as "Name: value" lines, blank values omitted.</summary>
  public static string ClipboardText(AssemblyInfo assemblyInfo)
  {
    ArgumentNullException.ThrowIfNull(assemblyInfo);
    IEnumerable<(string Name, string Value)> rows =
      NonBlank((nameof(AssemblyInfo.Title), assemblyInfo.Title))
        .Concat(Properties(assemblyInfo))
        .Concat
        (
          NonBlank
          (
            (nameof(AssemblyInfo.CommitUrl), assemblyInfo.CommitUrl),
            (nameof(AssemblyInfo.RepositoryUrl), assemblyInfo.RepositoryUrl)
          )
        );
    return string.Join("\n", rows.Select(row => $"{row.Name}: {row.Value}"));
  }

  private static List<(string Name, string Value)> NonBlank(params (string Name, string? Value)[] rows) =>
    [.. rows.Where(row => !string.IsNullOrWhiteSpace(row.Value)).Select(row => (row.Name, row.Value!))];
}
