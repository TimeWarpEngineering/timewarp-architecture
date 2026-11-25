#region Purpose
// Pins the dev CLI's feature checks: each marker is a path template.json removes with its flag off,
// any other exclude covering a marker is conditioned only on the feature's own or a required flag,
// this repository has every feature, a missing required feature is named first, and the refusal
// names the verb, the flag and the marker.
#endregion

namespace AppFeatures_;

using System.Text.Json;

public class Markers_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Markers_Given_>();

  public static Task EachMarker_Should_BeRemovedByItsFlagOff()
  {
    using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), ".template.config", "template.json")));
    JsonElement[] modifiers =
      [.. document.RootElement.GetProperty("sources").EnumerateArray().SelectMany(source => source.GetProperty("modifiers").EnumerateArray())];

    foreach (AppFeature feature in new[] { AppFeatures.Web, AppFeatures.Yarp, AppFeatures.Postgres })
    {
      string[] excludes =
      [
        .. modifiers
          .Where(modifier => modifier.GetProperty("condition").GetString() == $"(!{feature.Flag})")
          .SelectMany(modifier => modifier.GetProperty("exclude").EnumerateArray())
          .Select(entry => entry.GetString() ?? ""),
      ];

      excludes.Any(exclude => Covers(exclude, feature.MarkerPath)).ShouldBeTrue(feature.Flag);
    }

    return Task.CompletedTask;
  }

  // A marker another flag also removes would make the refusal name the wrong flag, unless the
  // feature declares that flag in Requires (checked first).
  public static Task EachMarker_Should_BeRemovedOnlyByItsOwnOrARequiredFlag()
  {
    using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), ".template.config", "template.json")));
    JsonElement[] modifiers =
      [.. document.RootElement.GetProperty("sources").EnumerateArray().SelectMany(source => source.GetProperty("modifiers").EnumerateArray())];

    foreach (AppFeature feature in new[] { AppFeatures.Web, AppFeatures.Yarp, AppFeatures.Postgres })
    {
      HashSet<string> allowed = [];
      for (AppFeature? current = feature; current is not null; current = current.Requires)
      {
        allowed.Add(current.Flag);
      }

      foreach (JsonElement modifier in modifiers)
      {
        if (!modifier.TryGetProperty("exclude", out JsonElement exclude)) continue;
        if (!exclude.EnumerateArray().Any(entry => Covers(entry.GetString() ?? "", feature.MarkerPath))) continue;

        string condition = modifier.GetProperty("condition").GetString() ?? "";
        string[] flags =
          [
            .. condition.Split(['(', ')', '|', '&', ' '], StringSplitOptions.RemoveEmptyEntries)
              .Where(token => token.StartsWith('!', StringComparison.Ordinal))
              .Select(token => token[1..]),
          ];

        flags.ShouldNotBeEmpty($"{feature.Flag}: {condition}");
        flags.ShouldAllBe(flag => allowed.Contains(flag), $"{feature.Flag} marker is also removed by {condition}");
      }
    }

    return Task.CompletedTask;
  }

  public static Task MissingRequiredFeature_Should_BeNamedFirst()
  {
    string root = Directory.CreateTempSubdirectory("app-features-").FullName;
    try
    {
      AppFeatures.FirstMissing(root, AppFeatures.Postgres).ShouldBe(AppFeatures.Web);

      Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, AppFeatures.Web.MarkerPath))!);
      File.WriteAllText(Path.Combine(root, AppFeatures.Web.MarkerPath), "");
      AppFeatures.FirstMissing(root, AppFeatures.Postgres).ShouldBe(AppFeatures.Postgres);

      Directory.CreateDirectory(Path.Combine(root, AppFeatures.Postgres.MarkerPath));
      AppFeatures.FirstMissing(root, AppFeatures.Postgres).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task TemplateRepository_Should_HaveEveryFeature()
  {
    string root = RepoRoot();
    AppFeatures.IsPresent(root, AppFeatures.Web).ShouldBeTrue();
    AppFeatures.IsPresent(root, AppFeatures.Yarp).ShouldBeTrue();
    AppFeatures.IsPresent(root, AppFeatures.Postgres).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task EmptyCheckout_Should_HaveNoFeature()
  {
    string root = Directory.CreateTempSubdirectory("app-features-").FullName;
    try
    {
      AppFeatures.IsPresent(root, AppFeatures.Web).ShouldBeFalse();
      AppFeatures.IsPresent(root, AppFeatures.Yarp).ShouldBeFalse();
      AppFeatures.IsPresent(root, AppFeatures.Postgres).ShouldBeFalse();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    return Task.CompletedTask;
  }

  public static Task MissingMessage_Should_NameVerbFlagAndMarker()
  {
    string message = AppFeatures.MissingMessage("dev open", AppFeatures.Yarp);

    message.ShouldStartWith("dev open needs the yarp feature.");
    message.ShouldContain(AppFeatures.Yarp.MarkerPath);
    message.ShouldContain("--yarp false");
    return Task.CompletedTask;
  }

  // A template.json exclude covers a path when it names it or is a "dir/**" glob above it.
  private static bool Covers(string exclude, string path) =>
    exclude == path
    || (exclude.EndsWith("/**", StringComparison.Ordinal) && path.StartsWith(exclude[..^2], StringComparison.Ordinal));

  private static string RepoRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
