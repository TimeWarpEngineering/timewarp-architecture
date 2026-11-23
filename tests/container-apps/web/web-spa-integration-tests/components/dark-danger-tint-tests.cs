#region Purpose
// The dark theme re-declares --twe-danger-tint. color-mix resolves on the element that
// declares the property, so a tint that exists only on :root stays the light pink.
#endregion

#region Design
// tokens.css is the source. The dark block is the body[data-theme="dark"] rule. Both
// the light root and that block declare the same 12% mix. 18% of dark danger into dark
// paper drops the error text under 4.5:1, so the percentage is part of the assertion.
#endregion

namespace DarkDangerTint_;

[TestTag("Unit")]
public class DarkDangerTint_Should_
{
  private const string DangerTint =
    "--twe-danger-tint: color-mix(in srgb, var(--twe-danger) 12%, var(--twe-paper));";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DarkDangerTint_Should_>();

  public static Task Declare_The_Danger_Tint_On_The_Dark_Theme()
  {
    string tokens = File.ReadAllText(Path.Combine(
      RepoRoot(),
      "source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css"));

    int count = 0;
    int index = 0;
    while ((index = tokens.IndexOf(DangerTint, index, StringComparison.Ordinal)) >= 0)
    {
      count++;
      index += DangerTint.Length;
    }

    count.ShouldBe(2);
    DarkBlock(tokens).ShouldContain(DangerTint);
    return Task.CompletedTask;
  }

  private static string DarkBlock(string tokens)
  {
    const string marker = "body[data-theme=\"dark\"]";
    int start = tokens.IndexOf(marker, StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0);
    int open = tokens.IndexOf('{', start, StringComparison.Ordinal);
    int depth = 0;
    for (int i = open; i < tokens.Length; i++)
    {
      if (tokens[i] == '{')
      {
        depth++;
      }
      else if (tokens[i] == '}')
      {
        depth--;
        if (depth == 0)
        {
          return tokens[open..(i + 1)];
        }
      }
    }

    return string.Empty;
  }

  private static string RepoRoot()
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return directory;
  }
}
