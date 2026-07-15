#region Purpose
// Enforces the agent-context-regions convention: every source file carries a #region Purpose block.
#endregion

#region Design
// The rule is deliberately UNIVERSAL rather than "non-trivial files only": any triviality
// heuristic (member counts, line thresholds) misclassifies in both directions and pushes
// complexity into suppressions. Trivial files satisfy the rule with a one-line Purpose instead
// of an exemption, so the check needs no judgment. Generated code is excluded structurally via
// GeneratedCodeAnalysisFlags.None (auto-generated headers, *.g.cs), not by heuristic.
// A #region Design block is intentionally NOT enforced — "has decisions worth recording" cannot
// be judged mechanically; that half of the convention is governed by AGENTS.md and review.
// Placement anywhere in the file counts: position is skill guidance, not analyzer scope.
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PurposeRegionAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0004";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Source file lacks a #region Purpose block",
      messageFormat: "File '{0}' has no '#region Purpose' block; add one (a single // line suffices) stating what the file is for",
      category: "Documentation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every source file embeds agent/human context in a '#region Purpose' block. Trivial files use a one-line Purpose rather than being exempt. See the agent-context-regions skill."
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

    bool hasPurposeRegion = root
      .DescendantNodes(descendIntoTrivia: true)
      .OfType<RegionDirectiveTriviaSyntax>()
      .Any(static region => RegionName(region) == "Purpose");

    if (hasPurposeRegion) return;

    // Anchor on the first token (not position 0): a diagnostic located before a leading
    // `#pragma warning disable` could never be suppressed by it. Trivia-only files (fully
    // commented out, or entirely inside an excluded preprocessor branch) have no active code
    // and are not flagged.
    SyntaxToken firstToken = root.GetFirstToken();
    if (firstToken.RawKind == 0) return;

    string fileName = Path.GetFileName(context.Tree.FilePath);
    context.ReportDiagnostic(Diagnostic.Create(Rule, firstToken.GetLocation(), fileName));
  }

  // "#region Purpose" -> "Purpose" (the name rides in the directive's trailing preprocessing message).
  private static string RegionName(RegionDirectiveTriviaSyntax region)
  {
    string text = region.ToString();
    int keywordEnd = text.IndexOf("#region", System.StringComparison.Ordinal);
    return keywordEnd < 0 ? string.Empty : text.Substring(keywordEnd + "#region".Length).Trim();
  }
}
