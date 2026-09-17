#region Purpose
// Fail if a Handler under web-spa/features/**/*-state/*.cs awaits another *State action.
#endregion

#region Design
// Task 236: handlers never chain actions; pages sequence. A source scan is cheaper than a
// host and matches the raw-button guard. Allow-list is empty. Pattern is
// `await [A-Za-z]+State.` inside a class named Handler.
#endregion

namespace HandlerNestedDispatchGuard_;

[TestTag("Unit")]
public class StateHandlers_Should_
{
  private static readonly string[] AllowListedRelativePaths = [];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StateHandlers_Should_>();

  public static Task Not_Await_Another_State_Action()
  {
    string repoRoot = FindRepoRoot();
    string featuresDirectory = Path.Combine(
      repoRoot,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "features");
    Directory.Exists(featuresDirectory).ShouldBeTrue(featuresDirectory);

    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(featuresDirectory, "*.cs", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(featuresDirectory, file).Replace('\\', '/');
      if (!relative.Contains("-state/", StringComparison.Ordinal)
        || !relative.EndsWith(".cs", StringComparison.Ordinal))
      {
        continue;
      }

      if (IsAllowListed(relative))
      {
        continue;
      }

      string source = File.ReadAllText(file);
      foreach (string hit in FindNestedDispatchesInHandlers(source))
      {
        offenders.Add($"{relative}: {hit}");
      }
    }

    offenders.ShouldBeEmpty(
      "Handlers must not dispatch another action (await XState.Y). Pages sequence. Offenders: "
      + string.Join("; ", offenders));
    return Task.CompletedTask;
  }

  private static IEnumerable<string> FindNestedDispatchesInHandlers(string source)
  {
    int searchFrom = 0;
    while (true)
    {
      int classIndex = IndexOfHandlerClass(source, searchFrom);
      if (classIndex < 0)
      {
        yield break;
      }

      int openBrace = source.IndexOf('{', classIndex);
      if (openBrace < 0)
      {
        yield break;
      }

      int closeBrace = MatchingCloseBrace(source, openBrace);
      if (closeBrace < 0)
      {
        yield break;
      }

      string body = source[(openBrace + 1)..closeBrace];
      foreach (string hit in FindAwaitStateDispatches(body))
      {
        yield return hit;
      }

      searchFrom = closeBrace + 1;
    }
  }

  private static IEnumerable<string> FindAwaitStateDispatches(string body)
  {
    int searchFrom = 0;
    while (true)
    {
      int awaitIndex = body.IndexOf("await ", searchFrom, StringComparison.Ordinal);
      if (awaitIndex < 0)
      {
        yield break;
      }

      int identifierStart = awaitIndex + "await ".Length;
      while (identifierStart < body.Length && char.IsWhiteSpace(body[identifierStart]))
      {
        identifierStart++;
      }

      int identifierEnd = identifierStart;
      while (identifierEnd < body.Length && char.IsAsciiLetter(body[identifierEnd]))
      {
        identifierEnd++;
      }

      if (identifierEnd > identifierStart
        && identifierEnd + "State.".Length <= body.Length
        && body.AsSpan(identifierEnd).StartsWith("State.", StringComparison.Ordinal))
      {
        yield return body[awaitIndex..(identifierEnd + "State.".Length)];
      }

      searchFrom = awaitIndex + 1;
    }
  }

  private static int IndexOfHandlerClass(string source, int start)
  {
    const string marker = "class Handler";
    return source.IndexOf(marker, start, StringComparison.Ordinal);
  }

  private static int MatchingCloseBrace(string source, int openBrace)
  {
    int depth = 0;
    for (int i = openBrace; i < source.Length; i++)
    {
      char c = source[i];
      if (c == '{')
      {
        depth++;
      }
      else if (c == '}')
      {
        depth--;
        if (depth == 0)
        {
          return i;
        }
      }
    }

    return -1;
  }

  private static bool IsAllowListed(string relativePath) =>
    AllowListedRelativePaths.Contains(relativePath, StringComparer.OrdinalIgnoreCase);

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }
}
