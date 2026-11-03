#region Purpose
// Emits [ApiRoute]/[AuthApiRequest]/[OpenDataQueryParameters] members on partial contract types,
// and copies [Operation] name, description, group, and tool hints onto that partial when the request carries them.
// Marker attributes are public types in TimeWarp.Foundation.Features (same namespace as
// IAuthApiRequest / HttpVerb) via RegisterPostInitializationOutput, then discovered with
// ForAttributeWithMetadataName. FastEndpoint/ingress match the same FQN — not per-consumer
// RootNamespace internals, and not simple-name string match.
#endregion

#region Design
// Attributes are public in TimeWarp.Foundation.Features so ForAttributeWithMetadataName can key a
// metadata name that does not vary with the generated app's RootNamespace (task 115 sourceName
// rewrite leaves TimeWarp.Foundation.* intact) and a foreign ApiRouteAttribute cannot collide.
// Discovery is one ForAttributeWithMetadataName per metadata name; the predicate requires a partial
// class (records/structs are skipped). The three pipelines are collected and merged into an
// equatable record struct ContractTarget (ImmutableArray parts) so trivia-only re-transforms do not re-emit,
// and one `{fqn}.g.cs` per type avoids AllowMultiple hint-name collisions. Extra same-kind
// attributes on one type are ignored after the first successful ContractPart (first-wins) so that merged
// file stays compilable; multi-route member APIs are out of scope.
// Static templates emit `GetRoute() => RouteTemplate` (no interpolation). Parameterized templates
// emit `GetRoute(...)` plus a parameterless `GetRoute()` that forwards to it so the format string
// is not duplicated. `[AuthApiRequest]` always emits `UserId`; `GetAuthQueryParameters` only when
// the same type is `IQueryStringRouteProvider` or also has `[OpenDataQueryParameters]` (list
// queries compose it; POST / GET-by-id keep the manual `IAuthApiRequest` form). Generated member
// types use `global::System.Guid` / `global::System.DateTime`.
// Route tokens are `{Name}` or `{Name:constraint}`. The colon is the only delimiter that starts a
// type/constraint token (task 053-003). An optional colon (`:?`) plus a required second `\w+` stole
// the last letter of identifiers that look like types (`{Date}` → Dat + e, `{LocationId}` → LocationI + d).
// Recognized constraint tokens and the C# type they emit:
//   guid → global::System.Guid; datetime → global::System.DateTime (GetRoute formats yyyy-MM-dd);
//   string / alpha / required / minlength* / maxlength* / length* / range* / regex* → string;
//   min* / max* (that did not match minlength/maxlength) → int;
//   any other token is used as the C# type as-is (int, long, bool, …);
//   omitted constraint → string.
// Constraint args are the parenthesized-digits form only (`min(1)`, `minlength(3)`). Multiple
// constraints, comma args, catch-alls, and `{name=default}` are not parsed.
// [Operation] (TimeWarp.Contracts.OperationAttribute) on the same partial is copied as
// OperationName and, when the matching named argument is not whitespace, OperationDescription,
// OperationGroup, and OperationTitle. DestructiveAttribute and OpenWorldAttribute are read by
// those metadata names. A present attribute copies OperationDestructive or OperationOpenWorld;
// an absent attribute omits the member, because unset is not false. A bare [Destructive] or
// [OpenWorld] (no constructor argument, or any value other than false) is true.
// A whitespace name supplies no operation, description, group, title, and hints included:
// the attribute constructor rejects that name, and EndpointEmitModel uses the same rule.
// Group, title, and the two hints are metadata. They are not inserted into RouteTemplate or
// GetRoute. Kind is not copied. The same type seen through more than one attribute pipeline
// keeps the first Operation part.
// Layout: this class only wires the pipeline. ContractsCollector builds the models in
// contracts-model.cs, and ContractsEmitter renders all source text.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

[Generator]
public sealed class ContractsGenerator : IIncrementalGenerator
{
  internal const string AttributeNamespace = "TimeWarp.Foundation.Features";
  internal const string ApiRouteAttributeMetadataName = AttributeNamespace + ".ApiRouteAttribute";
  internal const string AuthApiRequestAttributeMetadataName = AttributeNamespace + ".AuthApiRequestAttribute";
  internal const string OpenDataQueryParametersAttributeMetadataName = AttributeNamespace + ".OpenDataQueryParametersAttribute";

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    context.RegisterPostInitializationOutput(static ctx =>
      ctx.AddSource("ContractsGeneratorAttributes.g.cs", SourceText.From(ContractsEmitter.BuildAttributes(), Encoding.UTF8)));

    IncrementalValuesProvider<ContractTarget> routes = CreateAttributeProvider(
      context, ApiRouteAttributeMetadataName, static (ctx, _) => ContractsCollector.Transform(ctx, ContractsCollector.RouteName));
    IncrementalValuesProvider<ContractTarget> auths = CreateAttributeProvider(
      context, AuthApiRequestAttributeMetadataName, static (ctx, _) => ContractsCollector.Transform(ctx, ContractsCollector.AuthName));
    IncrementalValuesProvider<ContractTarget> openData = CreateAttributeProvider(
      context, OpenDataQueryParametersAttributeMetadataName, static (ctx, _) => ContractsCollector.Transform(ctx, ContractsCollector.OpenDataName));

    IncrementalValuesProvider<ContractTarget> merged = routes.Collect()
      .Combine(auths.Collect())
      .Combine(openData.Collect())
      .SelectMany(static (tuple, _) => ContractsCollector.MergeTargets(tuple.Left.Left, tuple.Left.Right, tuple.Right));

    context.RegisterSourceOutput(merged, static (spc, t) =>
      spc.AddSource($"{t.HintBase}.g.cs", SourceText.From(ContractsEmitter.Wrap(t), Encoding.UTF8)));
  }

  private static IncrementalValuesProvider<ContractTarget> CreateAttributeProvider(
    IncrementalGeneratorInitializationContext context,
    string metadataName,
    Func<GeneratorAttributeSyntaxContext, CancellationToken, ContractTarget?> transform)
  {
    return context.SyntaxProvider
      .ForAttributeWithMetadataName(
        metadataName,
        predicate: static (node, _) => IsPartialClass(node),
        transform: transform)
      .Where(static t => t is not null)
      .Select(static (t, _) => t!.Value);
  }

  private static bool IsPartialClass(SyntaxNode node) =>
    node is ClassDeclarationSyntax classDeclaration
    && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
}
