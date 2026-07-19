#region Purpose
// Enforces the nested-Invariants declaration half of the aggregate pattern: an aggregate root must
// declare a nested Invariants validator (TWA0011), kept private so it stays out of
// AddValidatorsFromAssemblyContaining auto-registration (TWA0012).
#endregion

#region Design
// Name-based detection (same approach as ContractNullabilityValidatorAnalyzer/SliceIsolationAnalyzer):
// matches types implementing an interface simple-named "IAggregateRoot" (via AllInterfaces, so
// indirect implementation — through a base class or through another interface that itself extends
// IAggregateRoot — is covered) without a hard compile-time reference to TimeWarp.Foundation.Entities.
// Abstract classes are exempt — an abstract aggregate base has no invariants of its own; concrete
// leaves are still checked. This is a deliberate decision, not an oversight: TWA0011 requires EVERY
// concrete leaf to declare its OWN nested validator (checked only against `aggregateType.GetTypeMembers()`
// — directly nested members, not inherited ones), even when a base class already declares one.
// DomainInvariantsGuard's runtime discovery additionally walks the BaseType chain and CAN resolve a
// base-declared validator via IValidator<in T> contravariance (see its Design region) — that
// walk exists to support EF dynamic proxies (runtime-generated types the analyzer never sees), and
// tolerates the base-declared-validator shape as a side effect, not as a second sanctioned authoring
// pattern. A hand-authored aggregate that relies on a base class's validator instead of declaring its
// own therefore still fails TWA0011 under warnings-as-errors even though it would pass the runtime
// guard — that asymmetry is intentional: this analyzer's contract is "every concrete leaf declares
// its own invariants," full stop.
// A "nested Invariants validator" is a type nested directly on the aggregate that is: not abstract,
// has a parameterless constructor of any accessibility, and derives a base named "AbstractValidator"
// with one type argument equal to the containing (aggregate) type, where that base's containing
// namespace is literally "FluentValidation" — this closes the gap where an unrelated same-named
// AbstractValidator<T> (or an abstract/ctor-parameterized one) would satisfy TWA0011 at build
// time yet throw MissingInvariantsValidatorException at DomainInvariantsGuard.EnsureValid. Matching
// by base-chain shape (not just the name "Invariants"), so a same-named nested type that is not
// actually a validator does not silently satisfy the rule. This is intentionally the SAME shape
// DomainInvariantsGuard checks at runtime (!IsAbstract, parameterless ctor, IValidator<T>
// assignable) with one addition here (the FluentValidation-namespace check) that the guard does not
// need, because the guard also verifies the discovered type is actually assignable to
// IValidator<T> via reflection — a check the analyzer approximates by name since it works
// purely from symbols and cannot assume FluentValidation's real IValidator<T> is referenced/
// resolvable in every compilation this analyzer runs against (it ships as an analyzer-only package
// safe to reference from contract projects that may not carry a FluentValidation package reference
// at all). Both sides remain out of sync for generic aggregate roots (`Aggregate<T> :
// IAggregateRoot`): the guard's reflection-based GetNestedTypes() on a closed generic type returns
// open nested-type definitions and fails discovery even when a validator is declared; no aggregate
// in this template is generic today.
// TWA0012 enumerates EVERY qualifying nested validator on the aggregate (not just the first) and
// reports on each one that is not private, since a second, non-private duplicate is exactly the
// harm the rule exists to catch — a leftover public copy left behind during a refactor can be
// picked up by assembly scanning even when a correct private one also exists.
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
  private const string FluentValidationNamespace = "FluentValidation";

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
      messageFormat: "'{0}.{1}' must be private; a non-private nested validator can be picked up by assembly scanning (such as AddValidatorsFromAssemblyContaining) and run a second time as a request validator",
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

    List<INamedTypeSymbol> validators = FindInvariantsValidators(type);
    if (validators.Count == 0)
    {
      Location location = type.Locations.FirstOrDefault() ?? Location.None;
      context.ReportDiagnostic(Diagnostic.Create(MissingInvariants, location, type.Name));
      return;
    }

    foreach (INamedTypeSymbol validator in validators)
    {
      if (validator.DeclaredAccessibility != Accessibility.Private)
      {
        Location location = validator.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(NonPrivateInvariants, location, type.Name, validator.Name));
      }
    }
  }

  private static bool ImplementsAggregateRoot(INamedTypeSymbol type) =>
    type.AllInterfaces.Any(static i => i.Name == AggregateRootInterfaceName);

  private static List<INamedTypeSymbol> FindInvariantsValidators(INamedTypeSymbol aggregateType) =>
    aggregateType.GetTypeMembers()
      .Where(nested => ValidatesAggregate(nested, aggregateType))
      .ToList();

  private static bool ValidatesAggregate(INamedTypeSymbol candidate, INamedTypeSymbol aggregateType)
  {
    if (candidate.IsAbstract) return false;
    if (!HasParameterlessConstructor(candidate)) return false;

    for (INamedTypeSymbol? baseType = candidate.BaseType; baseType is not null; baseType = baseType.BaseType)
    {
      if (baseType.Name == InvariantsValidatorBaseName
        && baseType.TypeArguments.Length == 1
        && SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[0], aggregateType)
        && baseType.ContainingNamespace?.ToDisplayString() == FluentValidationNamespace)
      {
        return true;
      }
    }

    return false;
  }

  // Any accessibility qualifies — DomainInvariantsGuard instantiates via
  // Activator.CreateInstance(validatorType, nonPublic: true). A type with no explicit constructor
  // has a synthesized public parameterless one, which InstanceConstructors includes.
  private static bool HasParameterlessConstructor(INamedTypeSymbol candidate) =>
    candidate.InstanceConstructors.Any(static ctor => ctor.Parameters.Length == 0);
}
