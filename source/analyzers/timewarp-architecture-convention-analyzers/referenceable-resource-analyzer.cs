#region Purpose
// Enforces the [ReferenceableResource] contract shape (TWA0033) and unique @ prefixes (TWA0034).
#endregion

#region Design
// Task 345. Runs on the contracts compilation, where the attribute is written, so the warning
// lands on the attribute. Shape rules live in ReferenceableResourceShape (shared with the SPA
// registry generator, which skips any contract this analyzer flags). TWA0034 is a compilation-end
// check: every marked contract with the same prefix is reported, since none of them is the
// obvious winner. Prefixes are compared ordinally; the shape rule already forces lowercase.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>Roslyn analyzer for TWA0033/TWA0034: referenceable resource contract shape and prefix uniqueness.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReferenceableResourceAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0033 (bad referenceable resource shape).</summary>
  public const string ShapeId = "TWA0033";
  /// <summary>Diagnostic identifier TWA0034 (duplicate @ prefix).</summary>
  public const string DuplicatePrefixId = "TWA0034";

  private const string Category = "Design";

  private static readonly DiagnosticDescriptor Shape =
    new
    (
      ShapeId,
      title: "[ReferenceableResource] contract has the wrong shape",
      messageFormat: "Contract '{0}' is marked [ReferenceableResource] but {1}; it is left out of the @ registry",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A referenceable resource is a list query the Ask composer calls as the user: [ApiEndpoint] + [EndpointAuthorize(Policy = ...)], a public nested IApiRequest Query with a public parameterless constructor, a settable string Search and no other required member, a public nested Response class with an enumerable items property of a public item type, and items carrying a [TypedId] or Guid IdProperty and a string LabelProperty."
    );

  private static readonly DiagnosticDescriptor DuplicatePrefix =
    new
    (
      DuplicatePrefixId,
      title: "Duplicate [ReferenceableResource] prefix",
      messageFormat: "Prefix '{0}' on '{1}' is also used by {2}; @ prefixes must be unique",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Each @prefix:id token names one resource type, so two contracts cannot share a prefix.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Shape, DuplicatePrefix);

  /// <summary>Registers the per-contract shape check and the compilation-end prefix check.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static start =>
    {
      ConcurrentBag<(string Prefix, string Contract, Location Location)> prefixes = [];
      start.RegisterSymbolAction(symbolContext => Analyze(symbolContext, prefixes), SymbolKind.NamedType);
      start.RegisterCompilationEndAction(endContext => ReportDuplicates(endContext, prefixes));
    });
  }

  private static void Analyze
  (
    SymbolAnalysisContext context,
    ConcurrentBag<(string Prefix, string Contract, Location Location)> prefixes
  )
  {
    var type = (INamedTypeSymbol)context.Symbol;
    AttributeData? attribute = ReferenceableResourceShape.FindAttribute(type);
    if (attribute is null) return;

    Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
      ?? type.Locations.FirstOrDefault() ?? Location.None;

    var shape = ReferenceableResourceShape.Read(type, attribute, out string? problem);
    if (shape is null)
    {
      context.ReportDiagnostic(Diagnostic.Create(Shape, location, type.ToDisplayString(), problem));
      return;
    }

    prefixes.Add((shape.Prefix, type.ToDisplayString(), location));
  }

  private static void ReportDuplicates
  (
    CompilationAnalysisContext context,
    ConcurrentBag<(string Prefix, string Contract, Location Location)> prefixes
  )
  {
    foreach (IGrouping<string, (string Prefix, string Contract, Location Location)> group in prefixes
      .GroupBy(static entry => entry.Prefix, System.StringComparer.Ordinal))
    {
      List<(string Prefix, string Contract, Location Location)> entries = [.. group.OrderBy(static e => e.Contract, System.StringComparer.Ordinal)];
      if (entries.Count < 2) continue;

      foreach ((string prefix, string contract, Location location) in entries)
      {
        string others = string.Join(", ", entries.Where(e => e.Contract != contract).Select(static e => "'" + e.Contract + "'"));
        context.ReportDiagnostic(Diagnostic.Create(DuplicatePrefix, location, prefix, contract, others));
      }
    }
  }
}
