#region Purpose
// Reports TWA0051 when an [ApiRoute] template breaks the route grammar: `api/` first, then
// lowercase kebab literal segments or whole-segment parameter tokens.
#endregion

#region Design
// Task 458-010: routes had split three ways (PascalCase `api/Feedback`, kebab `api/agent-links`,
// and `Analytics/TrackEvent` outside `api/`). The grammar lives in
// skills/tw-web-api-contracts/references/route-grammar.md; this analyzer is its build check, so
// the doc and the contracts cannot disagree.
// Every [ApiRoute] counts, hosted or [ClientOnlyContract]: a client-only route is still a wire
// path a server may host later. [ApiRoute] is matched by FQN through HostedRouteDiscovery.
// The template is read from AttributeData, so a const route (`[ApiRoute(Route, ...)]`) is checked
// by value. A template that is not a compile-time string is skipped (the compiler reports it).
// Grammar, checked left to right; the first break is the reason in the message:
//   - no leading `/` (GetRoute returns app-relative paths) and the first segment is `api`;
//   - no empty segment (`//` or a trailing `/`) and no query string;
//   - a segment is either one whole parameter token `{...}` (route-grammar.md owns its inner
//     grammar) or lowercase kebab: [a-z0-9]+ joined by single hyphens.
// Run-together words (`weatherforecast`) pass: kebab casing is checkable, word breaks are not.
// Location = the route argument in the attribute, so the squiggle sits on the string.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Roslyn analyzer for TWA0051: an [ApiRoute] template breaks the route grammar.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiRouteGrammarAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0051 ([ApiRoute] template breaks the route grammar).</summary>
  public const string RouteGrammarId = "TWA0051";

  private const string Category = "Design";
  private const string ApiPrefix = "api";

  private static readonly DiagnosticDescriptor RouteGrammar =
    new
    (
      RouteGrammarId,
      title: "[ApiRoute] template breaks the route grammar",
      messageFormat: "'{0}' route \"{1}\" breaks the route grammar: {2} — use `api/` then lowercase kebab segments (route-grammar.md)",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every [ApiRoute] template starts with `api/`. Each segment after it is lowercase kebab ([a-z0-9]+ joined by single hyphens) or one whole parameter token {Name} / {Name:constraint}. No leading slash, empty segment, or query string."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(RouteGrammar);

  /// <summary>Registers the symbol action that reports TWA0051.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
  }

  /// <summary>
  /// Returns null when <paramref name="route"/> follows the grammar, otherwise the first break.
  /// </summary>
  public static string? FindGrammarBreak(string route)
  {
    if (route.Length == 0)
      return "the template is empty";

    if (route[0] == '/')
      return "it starts with '/'";

    if (CountOf(route, '?') > 0)
      return "it contains a query string";

    string[] segments = route.Split('/');
    if (segments[0] != ApiPrefix)
      return "the first segment is not 'api'";

    for (int i = 1; i < segments.Length; i++)
    {
      string segment = segments[i];
      if (segment.Length == 0)
        return "it has an empty segment";

      if (IsParameterToken(segment))
        continue;

      if (!IsKebabSegment(segment))
        return $"segment '{segment}' is not lowercase kebab";
    }

    return segments.Length == 1 ? "it has no segment after 'api'" : null;
  }

  private static void Analyze(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;
    foreach (AttributeData attr in type.GetAttributes())
    {
      if (!HostedRouteDiscovery.IsApiRouteAttribute(attr))
        continue;

      if (attr.ConstructorArguments.Length == 0 || attr.ConstructorArguments[0].Value is not string route)
        continue;

      string? reason = FindGrammarBreak(route);
      if (reason is null)
        continue;

      Location location = RouteArgumentLocation(attr, context.CancellationToken)
        ?? type.Locations.FirstOrDefault()
        ?? Location.None;

      context.ReportDiagnostic(Diagnostic.Create(RouteGrammar, location, type.ToDisplayString(), route, reason));
    }
  }

  private static Location? RouteArgumentLocation(AttributeData attr, System.Threading.CancellationToken cancellationToken)
  {
    if (attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax syntax)
      return null;

    return syntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression.GetLocation()
      ?? syntax.GetLocation();
  }

  private static bool IsParameterToken(string segment) =>
    segment.Length > 2
    && segment[0] == '{'
    && segment[segment.Length - 1] == '}'
    && CountOf(segment, '{') == 1
    && CountOf(segment, '}') == 1;

  private static int CountOf(string text, char value)
  {
    int count = 0;
    foreach (char c in text)
    {
      if (c == value)
        count++;
    }

    return count;
  }

  private static bool IsKebabSegment(string segment)
  {
    bool previousWasHyphen = true;
    foreach (char c in segment)
    {
      if (c == '-')
      {
        if (previousWasHyphen)
          return false;

        previousWasHyphen = true;
        continue;
      }

      if (c is not ((>= 'a' and <= 'z') or (>= '0' and <= '9')))
        return false;

      previousWasHyphen = false;
    }

    return !previousWasHyphen;
  }
}
