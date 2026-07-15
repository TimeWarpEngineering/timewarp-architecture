#region Purpose
// A conditional-compilation directive naming a template.json flag requires that flag in the
// project's DefineConstants — otherwise the region silently vanishes from the real build (TWA0010).
#endregion

#region Design
// Template flag regions are kept in the repo build only because each csproj hand-lists the flags
// in DefineConstants; template.json is the authority for which symbols exist. Nothing else checks
// that agreement — miss a constant and the real build compiles WITHOUT a region the template
// still emits into generated apps (silent drift; found live on web-infrastructure during 089).
// Only this direction is checked. The reverse ("constant defined but no directive uses it") would
// false-positive: razor comment-conditionals and csproj XML-conditionals also consume the flags
// and are invisible to the C# compilation (web-spa defines `web` for exactly that reason).
// template.json arrives via AdditionalFiles (wired in source/ and tests/ Directory.Build.props,
// Exists-conditioned). Generated apps have neither the file nor surviving flag directives — the
// engine strips directive lines during generation — so the analyzer is naturally silent there.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;
using System.Text.Json;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TemplateFlagConstantsAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0010";

  private const string TemplateFileName = "template.json";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Directive uses a template flag missing from DefineConstants",
      messageFormat: "Directive condition uses template flag '{0}' but the project does not define it; add '{0}' to DefineConstants or this region silently vanishes from the repo build",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The dotnet-new template's feature-flag regions are kept in the repository build only when the project defines the flag as a preprocessor symbol. template.json is the authority for which flags exist; a directive naming an undefined flag means the repository compiles without code the template ships."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      ImmutableHashSet<string> templateFlags = ReadTemplateFlags(startContext.Options, startContext.CancellationToken);
      if (templateFlags.IsEmpty) return;

      startContext.RegisterSyntaxTreeAction(treeContext => AnalyzeTree(treeContext, templateFlags));
    });
  }

  private static void AnalyzeTree(SyntaxTreeAnalysisContext context, ImmutableHashSet<string> templateFlags)
  {
    var definedSymbols = new HashSet<string>(
      ((CSharpParseOptions)context.Tree.Options).PreprocessorSymbolNames,
      System.StringComparer.Ordinal);

    SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);

    foreach (ConditionalDirectiveTriviaSyntax directive in
      root.DescendantNodes(descendIntoTrivia: true).OfType<ConditionalDirectiveTriviaSyntax>())
    {
      if (directive.Condition is null) continue;

      foreach (IdentifierNameSyntax identifier in
        directive.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
      {
        string name = identifier.Identifier.ValueText;
        if (!templateFlags.Contains(name) || definedSymbols.Contains(name)) continue;

        context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), name));
      }
    }
  }

  // Bool symbols from template.json (delivered as an AdditionalFile); empty when absent/unreadable.
  private static ImmutableHashSet<string> ReadTemplateFlags(AnalyzerOptions options, System.Threading.CancellationToken cancellationToken)
  {
    AdditionalText? templateFile = options.AdditionalFiles.FirstOrDefault(
      static file => Path.GetFileName(file.Path).Equals(TemplateFileName, System.StringComparison.OrdinalIgnoreCase));

    string? json = templateFile?.GetText(cancellationToken)?.ToString();
    if (json is null) return ImmutableHashSet<string>.Empty;

    try
    {
      using var document = JsonDocument.Parse(json);
      if (!document.RootElement.TryGetProperty("symbols", out JsonElement symbols))
      {
        return ImmutableHashSet<string>.Empty;
      }

      ImmutableHashSet<string>.Builder flags = ImmutableHashSet.CreateBuilder<string>(System.StringComparer.Ordinal);
      foreach (JsonProperty symbol in symbols.EnumerateObject())
      {
        if (symbol.Value.TryGetProperty("datatype", out JsonElement datatype) && datatype.GetString() == "bool")
        {
          flags.Add(symbol.Name);
        }
      }

      return flags.ToImmutable();
    }
    catch (JsonException)
    {
      return ImmutableHashSet<string>.Empty;
    }
  }
}
