namespace TimeWarp.Architecture.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PartialClassDeclarationAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWPA0001";

  private static readonly LocalizableString Title = "Incorrect partial class declaration";
  private static readonly LocalizableString MessageFormat = "Partial class '{0}' {1}";
  private static readonly LocalizableString Description = "Partial classes should have one primary declaration in the main file with full specifiers and inheritance, while secondary files should have minimal declaration without class inheritance.";
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

      AnalyzeDeclaration(context, namedTypeSymbol, classSyntax, declaration.SyntaxTree);
    }
  }

  private static void AnalyzeDeclaration(SymbolAnalysisContext context, INamedTypeSymbol namedTypeSymbol, ClassDeclarationSyntax classSyntax, SyntaxTree sourceTree)
  {
    string filePath = sourceTree.FilePath;
    string? fileName = Path.GetFileName(filePath);

    bool isPrimaryFile = fileName.Equals($"{namedTypeSymbol.Name}.cs", StringComparison.OrdinalIgnoreCase);

    if (isPrimaryFile)
    {
      AnalyzePrimaryFile(context, namedTypeSymbol, classSyntax);
    }
    else if (fileName.StartsWith($"{namedTypeSymbol.Name}.", StringComparison.OrdinalIgnoreCase))
    {
      AnalyzeSecondaryFile(context, namedTypeSymbol, classSyntax);
    }
    else
    {
      ReportIncorrectFileName(context, namedTypeSymbol, classSyntax, fileName);
    }
  }

  private static void AnalyzePrimaryFile(SymbolAnalysisContext context, ISymbol symbol, BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
  {
    if (HasFullSpecifiers(baseTypeDeclarationSyntax)) return;
    var diagnostic = Diagnostic.Create(Rule, baseTypeDeclarationSyntax.Identifier.GetLocation(),
      symbol.Name, "should have full specifiers in the primary file");
    context.ReportDiagnostic(diagnostic);
  }

  private static void AnalyzeSecondaryFile(SymbolAnalysisContext context, ISymbol symbol, BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
  {
    if (HasExcessiveSpecifiers(baseTypeDeclarationSyntax))
    {
      var diagnostic = Diagnostic.Create(Rule, baseTypeDeclarationSyntax.Identifier.GetLocation(),
        symbol.Name, "should have minimal specifiers in secondary files");
      context.ReportDiagnostic(diagnostic);
    }

    if (HasClassInheritance(baseTypeDeclarationSyntax))
    {
      var diagnostic = Diagnostic.Create(Rule, baseTypeDeclarationSyntax.BaseList?.GetLocation() ?? baseTypeDeclarationSyntax.GetLocation(),
        symbol.Name, "should not include class inheritance in secondary files");
      context.ReportDiagnostic(diagnostic);
    }
  }

  private static void ReportIncorrectFileName(SymbolAnalysisContext context, ISymbol symbol, BaseTypeDeclarationSyntax baseTypeDeclarationSyntax, string? fileName)
  {
    var diagnostic = Diagnostic.Create(Rule, baseTypeDeclarationSyntax.Identifier.GetLocation(),
      symbol.Name, $"file name '{fileName}' does not follow the expected naming convention");
    context.ReportDiagnostic(diagnostic);
  }

  private static bool IsPartialType(ISymbol symbol) =>
    symbol.DeclaringSyntaxReferences.Length > 1
    || (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ClassDeclarationSyntax classDeclaration
    && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword));

  private static bool HasFullSpecifiers(MemberDeclarationSyntax memberDeclarationSyntax) =>
    memberDeclarationSyntax.Modifiers.Any
    (
      m => m.IsKind(SyntaxKind.PublicKeyword)
        || m.IsKind(SyntaxKind.InternalKeyword)
        || m.IsKind(SyntaxKind.ProtectedKeyword)
        || m.IsKind(SyntaxKind.PrivateKeyword)
    );

  private static bool HasExcessiveSpecifiers(MemberDeclarationSyntax memberDeclarationSyntax) =>
    memberDeclarationSyntax.Modifiers.Any(m => !m.IsKind(SyntaxKind.PartialKeyword));

  private static bool HasClassInheritance(BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
  {
    if (baseTypeDeclarationSyntax.BaseList is not { Types.Count: > 0 })
      return false;

    return baseTypeDeclarationSyntax.BaseList.Types.Any(t =>
      (t.Type is IdentifierNameSyntax identifierName && !identifierName.Identifier.Text.StartsWith("I")) ||
      (t.Type is QualifiedNameSyntax qualifiedName && !qualifiedName.Right.Identifier.Text.StartsWith("I")));
  }
}
