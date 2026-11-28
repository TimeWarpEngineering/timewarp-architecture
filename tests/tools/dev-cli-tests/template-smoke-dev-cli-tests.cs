#region Purpose
// Pins ListedCommands: the capabilities JSON may start the output, follow warning lines, or follow
// an ANSI sequence on its own line. The first `dotnet run` of a generated dev CLI prints build
// warnings ahead of Nuru's JSON.
#endregion

namespace TemplateSmokeDevCli_;

using System.Text.Json;
using TimeWarp.Jaribu;

[TestTag("Unit")]
public class ListedCommands_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ListedCommands_Given_>();

  [Proves(typeof(TemplateSmokeDevCli), nameof(TemplateSmokeDevCli.ListedCommands))]
  public static Task JsonAtOffsetZero_Should_ListTheFirstWordOfEachPattern()
  {
    HashSet<string>? listed = TemplateSmokeDevCli.ListedCommands(Capabilities("build", "db update", "test"));

    listed.ShouldNotBeNull();
    listed.Order(StringComparer.Ordinal).ShouldBe(["build", "db", "test"]);
    return Task.CompletedTask;
  }

  [Proves(typeof(TemplateSmokeDevCli), nameof(TemplateSmokeDevCli.ListedCommands))]
  public static Task JsonAfterWarningLines_Should_ListTheCommands()
  {
    string stdout =
      "CSC : warning CS0000: a first dotnet run prints warnings before the JSON\n" +
      "another warning line\n" +
      Capabilities("build", "run");

    HashSet<string>? listed = TemplateSmokeDevCli.ListedCommands(stdout);

    listed.ShouldNotBeNull();
    listed.Order(StringComparer.Ordinal).ShouldBe(["build", "run"]);
    return Task.CompletedTask;
  }

  [Proves(typeof(TemplateSmokeDevCli), nameof(TemplateSmokeDevCli.ListedCommands))]
  public static Task JsonAfterAnsiOnTheSameLine_Should_ListTheCommands()
  {
    string stdout = "warning CS0000: building\n\u001b[33m" + Capabilities("publish", "db status");

    HashSet<string>? listed = TemplateSmokeDevCli.ListedCommands(stdout);

    listed.ShouldNotBeNull();
    listed.Order(StringComparer.Ordinal).ShouldBe(["db", "publish"]);
    return Task.CompletedTask;
  }

  private static string Capabilities(params string[] patterns)
  {
    string endpoints = string.Join(
      ',',
      patterns.Select(pattern => "{\"pattern\":" + JsonSerializer.Serialize(pattern) + "}"));
    return "{\"endpoints\":[" + endpoints + "]}";
  }
}
