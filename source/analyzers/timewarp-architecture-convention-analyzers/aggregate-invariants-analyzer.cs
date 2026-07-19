#region Purpose
// Enforces the nested-Invariants declaration half of the aggregate pattern: an aggregate root must
// declare a nested Invariants validator (TWA0011), kept private so it stays out of
// AddValidatorsFromAssemblyContaining auto-registration (TWA0012).
#endregion

#region Design
// Name-based detection (same approach as ContractNullabilityValidatorAnalyzer/SliceIsolationAnalyzer):
// matches types implementing an interface simple-named "IAggregateRoot" so the check works without a
// hard compile-time reference to TimeWarp.Foundation.Entities. Abstract classes are exempt — an
// abstract aggregate base has no invariants of its own; concrete leaves are still checked.
// A "nested Invariants validator" is any type nested directly on the aggregate whose base chain
// includes AbstractValidator&lt;T&gt; with T equal to the containing (aggregate) type — the same
// shape DomainInvariantsGuard discovers at runtime. Matching by base-chain shape, not by name, so
// the convention does not silently accept a same-named type that is not actually a validator.
// TWA0012 only fires once a qualifying validator was found (TWA0011 already covers "no validator at
// all"); it flags a public/internal/protected nested validator, since only private nesting is
// invisible to AddValidatorsFromAssemblyContaining.
// The runtime DomainInvariantsGuard fail-closed check (foundation-application) intentionally
// duplicates this: the analyzer is the build-time upgrade, the guard is the persistence-time
// backstop for anything the analyzer cannot see.
#endregion

namespace TimeWarp.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AggregateInvariantsAnalyzer : DiagnosticAnalyzer
{
  public const string MissingInvariantsId = "TWA0011";
  public const string NonPrivateInvariantsId = "TWA0012";

  private const string Category = "Design";
  private const string AggregateRootInterfaceName = "IAggregateRoot";
  private const string InvariantsValidatorBaseName = "AbstractValidator";

  private static readonly DiagnosticDescriptor MissingInvariants =
    new
    (
      MissingInvariantsId,
      title: "Aggregate root must declare a nested Invariants validator",
      messageFormat: "'{0}' implements IAggregateRoot but declares no nested Invariants validator (a nested class deriving AbstractValidator<{0}>)",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every aggregate root must declare its invariants so DomainInvariantsGuard can enforce them before a save; a root with no validator is itself a defect (fail-closed)."
    );

  private static readonly DiagnosticDescriptor NonPrivateInvariants =
    new
    (
      NonPrivateInvariantsId,
      title: "Nested Invariants validator must be private",
      messageFormat: "'{0}.{1}' must be private; a non-private nested validator is picked up by AddValidatorsFromAssemblyContaining and runs a second time as a request validator",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Invariants validators stay privately nested so contract-validator auto-registration does not also register them as request validators."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(MissingInvariants, NonPrivateInvariants);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
  }

  private static void AnalyzeNamedType(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;

    if (type.TypeKind != TypeKind.Class || type.IsAbstract) return;
    if (!ImplementsAggregateRoot(type)) return;

    INamedTypeSymbol? validator = FindInvariantsValidator(type);
    if (validator is null)
    {
      Location location = type.Locations.FirstOrDefault() ?? Location.None;
      context.ReportDiagnostic(Diagnostic.Create(MissingInvariants, location, type.Name));
      return;
    }

    if (validator.DeclaredAccessibility != Accessibility.Private)
    {
      Location location = validator.Locations.FirstOrDefault() ?? Location.None;
      context.ReportDiagnostic(Diagnostic.Create(NonPrivateInvariants, location, type.Name, validator.Name));
    }
  }

  private static bool ImplementsAggregateRoot(INamedTypeSymbol type) =>
    type.AllInterfaces.Any(static i => i.Name == AggregateRootInterfaceName);

  private static INamedTypeSymbol? FindInvariantsValidator(INamedTypeSymbol aggregateType) =>
    aggregateType.GetTypeMembers().FirstOrDefault(nested => ValidatesAggregate(nested, aggregateType));

  private static bool ValidatesAggregate(INamedTypeSymbol candidate, INamedTypeSymbol aggregateType)
  {
    for (INamedTypeSymbol? baseType = candidate.BaseType; baseType is not null; baseType = baseType.BaseType)
    {
      if (baseType.Name == InvariantsValidatorBaseName
        && baseType.TypeArguments.Length == 1
        && SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[0], aggregateType))
      {
        return true;
      }
    }

    return false;
  }
}
