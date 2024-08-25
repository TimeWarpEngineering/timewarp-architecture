namespace TimeWarp.Architecture.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PartialClassDeclarationAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWPA0001";

  private static readonly LocalizableString Title = "Incorrect partial class declaration";
  private static readonly LocalizableString MessageFormat = "Partial class '{0}' {1}";
  private static readonly LocalizableString Description = "Partial classes should have one primary declaration in the main file with full specifiers and inheritance, while secondary files should have minimal declaration without inheritance.";
  private const string Category = "Design";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      Title,
      MessageFormat,
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: Description,
      helpLinkUri: "https://github.com/TimeWarpEngineering/timewarp-architecture/blob/main/Documentation/Analyzers/TWPA0001.md"
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
  }

  private static void AnalyzeSymbol(SymbolAnalysisContext context)
  {
    var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

    if (!namedTypeSymbol.IsType || !IsPartialType(namedTypeSymbol))
      return;

    ImmutableArray<SyntaxReference> declarations = namedTypeSymbol.DeclaringSyntaxReferences;

    if (declarations.Length <= 1)
      return;

    foreach (SyntaxReference? declaration in declarations)
    {
      if (declaration.GetSyntax() is not ClassDeclarationSyntax classSyntax)
        continue;

      SyntaxTree sourceTree = declaration.SyntaxTree;
      string filePath = sourceTree.FilePath;
      string? fileName = Path.GetFileName(filePath);

      bool isPrimaryFile = fileName.Equals($"{namedTypeSymbol.Name}.cs", StringComparison.OrdinalIgnoreCase);

      if (isPrimaryFile)
      {
        if (HasFullSpecifiers(classSyntax)) continue;
        var diagnostic = Diagnostic.Create(Rule, classSyntax.Identifier.GetLocation(),
          namedTypeSymbol.Name, "should have full specifiers in the primary file");
        context.ReportDiagnostic(diagnostic);
      }
      else if (fileName.StartsWith($"{namedTypeSymbol.Name}.", StringComparison.OrdinalIgnoreCase))
      {
        if (HasExcessiveSpecifiers(classSyntax))
        {
          var diagnostic = Diagnostic.Create(Rule, classSyntax.Identifier.GetLocation(),
            namedTypeSymbol.Name, "should have minimal specifiers in secondary files");
          context.ReportDiagnostic(diagnostic);
        }

        if (HasInheritanceOrInterfaces(classSyntax))
        {
          var diagnostic = Diagnostic.Create(Rule, classSyntax.BaseList?.GetLocation() ?? classSyntax.GetLocation(),
            namedTypeSymbol.Name, "should not include inheritance or interfaces in secondary files");
          context.ReportDiagnostic(diagnostic);
        }
      }
      else
      {
        var diagnostic = Diagnostic.Create(Rule, classSyntax.Identifier.GetLocation(),
          namedTypeSymbol.Name, $"file name '{fileName}' does not follow the expected naming convention");
        context.ReportDiagnostic(diagnostic);
      }
    }
  }

  private static bool IsPartialType(ISymbol symbol)
  {
    return symbol.DeclaringSyntaxReferences.Length > 1
      || (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax classDeclaration
      && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword));
  }

  private static bool HasFullSpecifiers(MemberDeclarationSyntax memberDeclarationSyntax)
  {
    return memberDeclarationSyntax.Modifiers.Any
    (
      m => m.IsKind(SyntaxKind.PublicKeyword)
        || m.IsKind(SyntaxKind.InternalKeyword)
        || m.IsKind(SyntaxKind.ProtectedKeyword)
        || m.IsKind(SyntaxKind.PrivateKeyword)
    );
  }

  private static bool HasExcessiveSpecifiers(MemberDeclarationSyntax memberDeclarationSyntax) =>
    memberDeclarationSyntax.Modifiers.Any(m => !m.IsKind(SyntaxKind.PartialKeyword));

  private static bool HasInheritanceOrInterfaces(BaseTypeDeclarationSyntax baseTypeDeclarationSyntax) =>
    baseTypeDeclarationSyntax.BaseList is { Types.Count: > 0 };
}
