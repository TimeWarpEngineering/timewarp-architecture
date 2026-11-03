#region Purpose
// Emits the house typed-id BCL surface (Value/New/From/Json/Parse/TypeConverter) onto [TypedId]
// readonly partial record structs, and EF ValueConverters into assemblies that reference EF Core.
#endregion

#region Design
// Both injected attributes are INTERNAL: each compilation running the generator gets its own copy, so
// nothing is exported and two generator-running assemblies referencing each other cannot collide
// (a public copy caused CS0436 at every [TypedId] usage site — proven by probe, 104-027 review).
// Roslyn reads attribute *applications* from referenced-assembly metadata regardless of the attribute
// class's accessibility, so the EF pass still sees [TypedId] on referenced internal-attributed types.
// Matching is by simple name so each assembly's copy is equivalent.
// A [assembly: TypedIdsEmbedded] marker is stamped whenever a compilation declares typed ids; the EF
// pass walks only marked referenced assemblies instead of the whole reference closure (MSBuild passes
// the transitive closure to the compiler, so ReferencedAssemblySymbols covers transitive references —
// without the marker filter every framework assembly's namespace tree would be walked per run).
// The EF output is keyed on an equatable model (HasEf + sorted id list) so it only regenerates when
// the id set or EF-ness actually changes, not on every compilation edit.
// Referenced-assembly ids are validated by member shape (Value/From), not IsRecord — record structs
// compile to plain structs and IsRecord is false on metadata symbols.
// Invalid shapes (not a readonly partial record struct) report TWE006 as a build error: a silently
// skipped id would still compile as a contract property and serialize fail-open as {} — the exact
// defect this generator exists to close.
// Record-struct equality stays compiler-synthesized — do not emit Equals/GetHashCode/==/!=.
// Comparison operators (< <= > >=) are emitted for CA1036 once IComparable is implemented.
// Layout: this class only wires the pipeline. TypedIdCollector builds the models in
// typed-id-model.cs, and TypedIdEmitter renders all source text.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;

/// <summary>Source generator that emits the typed-id BCL surface and EF ValueConverters for [TypedId] structs.</summary>
[Generator]
public sealed class TypedIdSourceGenerator : IIncrementalGenerator
{
  /// <summary>Registers incremental generator steps for [TypedId] BCL and EF converter emission.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    context.RegisterPostInitializationOutput(static ctx =>
      ctx.AddSource("TypedIdAttribute.g.cs", SourceText.From(TypedIdEmitter.AttributeSource, Encoding.UTF8)));

    IncrementalValueProvider<ImmutableArray<TypedIdCandidate>> candidates = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => TypedIdCollector.IsCandidate(node),
        transform: static (ctx, ct) => TypedIdCollector.GetCandidate(ctx, ct))
      .Where(static c => c is not null)
      .Select(static (c, _) => c!.Value)
      .Collect();

    context.RegisterSourceOutput(candidates, static (spc, all) => TypedIdEmitter.EmitBclAndMarker(spc, all));

    IncrementalValueProvider<TypedIdEfModel> efModel = context.CompilationProvider
      .Combine(candidates)
      .Select(static (pair, ct) => TypedIdCollector.BuildEfModel(pair.Left, pair.Right, ct));

    context.RegisterSourceOutput(efModel, static (spc, model) =>
    {
      try
      {
        TypedIdEmitter.EmitEf(spc, model);
      }
      catch (Exception ex) // CA1031: source generators must be resilient
      {
        spc.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TypedIdEfGeneratorError, Location.None, ex.Message));
      }
    });
  }
}
