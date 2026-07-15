#region Purpose
// Flags template-conditional tokens (the if/elif/else/endif directives) inside comments or string
// literals, where the dotnet-new engine misreads them and truncates generated files (TWA0008).
#endregion

#region Design
// This repo IS the dotnet-new template: the engine runs C# conditional-processing over every
// generated .cs file and treats the directive tokens as live even inside comments and strings —
// an unmatched one strips content to the next end-directive or EOF, producing CS1513 in every
// generated app (the 086 incident). Real preprocessor directives are directive trivia, which this
// analyzer never scans, so only misread occurrences are flagged.
// Not an absolute ban: the engine's own escape (the cnd:noEmit comment marker pair) disables
// conditional processing for a region, so occurrences inside such a region are exempt — that is
// the sanctioned form for analyzer-test sources embedding directives, generators emitting them,
// and prose that genuinely needs the token. The diagnostic message names both remedies.
// Self-hosting: this file is itself template content, so the directive tokens and the full marker
// text are COMPOSED at runtime rather than written literally — the raw byte sequences must never
// appear here or the engine would mangle this very file at generation time.
// Known gap: the convention-analyzers project cannot analyze itself (a project cannot be its own
// analyzer), so THIS project's sources — where the original incident happened — are guarded only
// by the composition discipline above. Everything else in source/ and tests/ is enforced.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;
using System.Text.RegularExpressions;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TemplateConditionalTokenAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0008";

  // Composed so the raw token sequences never appear in this file (see Design).
  private const string Hash = "#";
  private const string DisableMarker = "-:cnd:noEmit"; // full form: "//" + DisableMarker
  private const string EnableMarker = "+:cnd:noEmit";

  private static readonly Regex TokenPattern =
    new(Hash + @"\s*(if|elif|else|endif)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Comment or string contains a template-conditional token",
      messageFormat:
        "'" + Hash + "{0}' inside a comment or string is processed by the dotnet-new engine and truncates the generated file; " +
        "reword it, or wrap the region in the //" + DisableMarker + " … //" + EnableMarker + " escape",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description:
        "This repository is the dotnet-new template. The template engine runs C# conditional processing " +
        "over generated .cs files and treats conditional-directive tokens as live even inside comments and " +
        "string literals, stripping content to the next end-directive or end-of-file. Reword the text, or " +
        "disable conditional processing for the region with the cnd:noEmit comment-marker pair."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxTreeAction(AnalyzeTree);
  }

  private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
  {
    SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);

    List<TextSpan> exemptSpans = CollectNoEmitSpans(root);

    foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true))
    {
      if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
      {
        continue;
      }

      ReportMatches(context, trivia.ToString(), trivia.SpanStart, exemptSpans);
    }

    foreach (SyntaxToken token in root.DescendantTokens(descendIntoTrivia: true))
    {
      if (!IsTextBearingToken(token))
      {
        continue;
      }

      ReportMatches(context, token.Text, token.SpanStart, exemptSpans);
    }
  }

  // String-literal token kinds in all their forms, plus doc-comment text (doc comments are
  // structured trivia, so their prose arrives as XML text tokens rather than comment trivia).
  private static bool IsTextBearingToken(SyntaxToken token) =>
    token.Kind() is
      SyntaxKind.StringLiteralToken or
      SyntaxKind.SingleLineRawStringLiteralToken or
      SyntaxKind.MultiLineRawStringLiteralToken or
      SyntaxKind.InterpolatedStringTextToken or
      SyntaxKind.Utf8StringLiteralToken or
      SyntaxKind.Utf8SingleLineRawStringLiteralToken or
      SyntaxKind.Utf8MultiLineRawStringLiteralToken or
      SyntaxKind.XmlTextLiteralToken;

  private static void ReportMatches(SyntaxTreeAnalysisContext context, string text, int offset, List<TextSpan> exemptSpans)
  {
    foreach (Match match in TokenPattern.Matches(text))
    {
      var matchSpan = new TextSpan(offset + match.Index, match.Length);
      if (exemptSpans.Any(exempt => exempt.Contains(matchSpan.Start)))
      {
        continue;
      }

      context.ReportDiagnostic(Diagnostic.Create(
        Rule,
        Location.Create(context.Tree, matchSpan),
        match.Groups[1].Value));
    }
  }

  // Spans between a disable marker comment and the next enable marker (or EOF) are exempt —
  // the template engine skips conditional processing there, so tokens inside are harmless.
  private static List<TextSpan> CollectNoEmitSpans(SyntaxNode root)
  {
    var spans = new List<TextSpan>();
    int? openStart = null;

    foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true))
    {
      if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
      {
        continue;
      }

      string body = trivia.ToString().TrimStart('/').Trim();
      if (body.StartsWith(DisableMarker, System.StringComparison.Ordinal))
      {
        openStart ??= trivia.SpanStart;
      }
      else if (body.StartsWith(EnableMarker, System.StringComparison.Ordinal) && openStart is int start)
      {
        spans.Add(TextSpan.FromBounds(start, trivia.Span.End));
        openStart = null;
      }
    }

    if (openStart is int unclosed)
    {
      spans.Add(TextSpan.FromBounds(unclosed, root.FullSpan.End));
    }

    return spans;
  }
}
