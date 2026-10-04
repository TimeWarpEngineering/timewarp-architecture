#region Purpose
// TWA0028: an Enumeration subclass's members must be public static readonly fields, the only shape
// Enumeration.GetAll discovers.
#endregion

#region Design
// Enumeration.GetAll<T> reflects over typeof(T).GetFields(Public | Static | DeclaredOnly), so a
// member declared as a static property, a non-public static field, or a mutable public static
// field either vanishes from every lookup (FromValue/FromName/FromString/JSON read) or can be
// reassigned after the cache captured it. This analyzer turns that agreement-by-memory into a
// build error.
// Name-based detection (same approach as AggregateInvariantsAnalyzer): a class whose base chain
// contains a type named "Enumeration" in namespace "TimeWarp.Foundation.Enumerations" — no hard
// reference to the foundation-domain assembly, so the analyzer package stays safe repo-wide.
// A "member" is a static field or static property declared directly on that class whose declared
// type is the class itself or a type deriving from it (CorsPolicy.Any is declared as CorsPolicy
// though its runtime type is the nested AnyPolicy). Implicitly declared symbols (property backing
// fields) are skipped — the property itself is reported. A deliberately hidden alias (a private
// static field pointing at an existing member) is the one legitimate exception; suppress it with
// #pragma at the declaration so the choice is visible.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Roslyn analyzer for TWA0028: Enumeration members must be declared as public static readonly fields.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnumerationMemberShapeAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0028.</summary>
  public const string DiagnosticId = "TWA0028";

  private const string Category = "Design";
  private const string EnumerationTypeName = "Enumeration";
  private const string EnumerationNamespace = "TimeWarp.Foundation.Enumerations";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Enumeration member must be a public static readonly field",
      messageFormat: "'{0}.{1}' is a member of Enumeration '{0}' but is declared as {2}; declare it as a public static readonly field or Enumeration.GetAll (and every From*/TryFrom*/JSON lookup) will not see it reliably",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Enumeration.GetAll discovers members by reflecting over public static fields declared on the subclass. A static property or non-public field silently vanishes from lookups; a non-readonly field can be reassigned after the member cache captured it."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  /// <summary>Registers the named-type action that reports TWA0028.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
  }

  private static void AnalyzeNamedType(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;

    if (type.TypeKind != TypeKind.Class) return;
    if (!DerivesFromEnumeration(type)) return;

    foreach (ISymbol member in type.GetMembers())
    {
      if (member.IsImplicitlyDeclared || !member.IsStatic) continue;

      string? shape = member switch
      {
        IFieldSymbol field when IsMemberType(field.Type, type) => DescribeFieldShape(field),
        IPropertySymbol property when IsMemberType(property.Type, type) => "a static property",
        _ => null
      };

      if (shape is null) continue;

      Location location = member.Locations.FirstOrDefault() ?? Location.None;
      context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name, member.Name, shape));
    }
  }

  // Null when the field already has the required shape.
  private static string? DescribeFieldShape(IFieldSymbol field)
  {
    bool isPublic = field.DeclaredAccessibility == Accessibility.Public;
    if (isPublic && field.IsReadOnly) return null;

    return isPublic ? "a mutable (non-readonly) field" : "a non-public field";
  }

  private static bool DerivesFromEnumeration(INamedTypeSymbol type)
  {
    for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
    {
      if (baseType.Name == EnumerationTypeName && baseType.ContainingNamespace?.ToDisplayString() == EnumerationNamespace)
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsMemberType(ITypeSymbol candidate, INamedTypeSymbol enumerationType)
  {
    for (ITypeSymbol? current = candidate; current is not null; current = current.BaseType)
    {
      if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, enumerationType.OriginalDefinition)) return true;
    }

    return false;
  }
}
