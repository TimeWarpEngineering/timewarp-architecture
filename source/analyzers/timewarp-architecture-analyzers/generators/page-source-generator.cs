#region Purpose
// For a Blazor page class marked [Page("/route"[, "/alias", …][, Policy = Policies.X])] generates [Route]s,
// INavigableComponent/IStaticRoute, GetPageUrl(...), Policy accessor, and [Parameter] props for
// route tokens; plus one per-assembly PageRegistry of the pages that opt in as navigation
// destinations (task 239-001). Origin: web-spa Page Moxy template, replaced in task 053.
#endregion

#region Design
// Policy is a pit-of-success const binding (task 094), not a free-form string:
// - Omitted → emit Policies.Anonymous (product must define that const).
// - Policy = Policies.SettingsEdit (member access / simple identifier const ref) → emit that
//   expression verbatim so claim values like "settings.edit" work without identifier glue.
// - String literals and nameof(...) are rejected with TWE005 — one authoring form only.
// - Never silently fall back to Anonymous when Policy was written but unparseable.
// Route stays a string literal (routes are inherently literal templates).
// Attribute Policy property remains string so const string fields are valid attribute args;
// generation keys off syntax, not the attribute's compile-time string value.
// PageRegistry (task 239-001) is the single source of navigation destinations for the NavMenu
// and the Ctrl-K palette (239-003) — no second hand-copied route list:
// - Opt-in is a [Page] property, `Navigable = true` (default false), not a separate attribute:
//   one attribute already owns route + policy, and a second marker could drift from it. Literal
//   true/false only (syntax-keyed like Policy); anything else is TWE009.
// - Only static routes qualify: a parameterized route needs arguments, so Navigable on one is
//   TWE009 (fail-closed, never a silent omission). v1 has no parameterized destinations.
// - An opt-in page also gets INavigationDestination, the marker the registry test checks both
//   ways. NavMenu renders the registry itself (categories from NavGroup, gates from each page's
//   Policy), so it has no page list of its own to drift.
// - Entries are a generated array of static member reads (typeof + GetPageUrl/Title/NavIcon/
//   Policy): reflection-free, AOT/trim safe. Sorted by NavOrder, then route (ordinal).
// - The registry is emitted only when the assembly declares at least one [Page], so assemblies
//   that merely carry the generator never need INavigableComponent's Icon type.
// Multi-route pages (task 096) — one [Page] with params additional routes, not stacked [Page]:
// - [Page("/clients", "/clients/revenue", "/clients/me-close")]. The first argument is the
//   PRIMARY route; the rest are ADDITIONAL routes. Stacked [Page] was rejected because each copy
//   could carry its own Policy/Navigable, so "which declaration owns the page" would need a rule;
//   one attribute keeps route + policy + opt-in in one place (same reason Navigable is a property).
//   AllowMultiple = false makes the language enforce it; TWE011 adds the teaching message.
// - Primary owns the page surface: GetPageUrl, IStaticRoute, RouteTemplate, PageRegistry, and the
//   TWE009 Navigable judgment. Additional routes are emitted as [Route] aliases only — never registry
//   rows, never URL helpers — so a parameterized alias on a static navigable page is fine.
// - PageRegistryEntry.RouteTemplates is init-only and lists every [Page] template, including
//   parameterized aliases. Nav match reads it. It is not a second registry row and not a URL
//   helper. The positional RouteTemplate stays the static primary.
// - Policy (TWE005) is per page, not per route: every route of the page is the same component.
// - An alias token named like an earlier route's token inherits its type when untyped ({ClientId}
//   after {ClientId:string}) and must agree when typed (else TWE011) — also between two aliases,
//   since the token gets one [Parameter] property. An alias-only token still gets that property,
//   since Blazor fails to bind a route value with no matching property.
// - TWE010: two routes of one page that Blazor treats as the same route (case-insensitive,
//   token-name-free shape), including a hand-written [Route] repeating a [Page] route. Distinct
//   hand-written [Route] aliases stay legal (the pre-096 workaround still compiles). Shape
//   normalization covers the {name} / {name:type} tokens RouteParam recognizes; catch-all ({*x}),
//   optional ({id?}) and parameterized-constraint tokens are not [Page] route grammar (no
//   [Parameter] is generated for them) and compare as literal text.
// - Errors (TWE005/010/011) are fail-closed: reported, and no page surface is emitted.
// Menu and agent metadata (task 300) — one declaration feeds NavMenu, Ctrl-K, Ask and WebMCP:
// - NavGroup (string constant, "" = top level) is the menu category; NavOrder (int constant) is
//   the menu position. PageRegistry.All is sorted by NavOrder, then route, so the registry order
//   IS the menu order and NavMenu starts a category whenever NavGroup changes; the registry test
//   requires each NavGroup to be one contiguous run. NavPlacement is Top (default) or Bottom.
//   Bottom pins that group in the lower nav. One named group uses one placement. Description,
//   NavGroup and NavOrder accept any compile-time constant. NavPlacement does not: this
//   generator emits the enum, so the syntax pass never binds it and GetConstantValue has no
//   value for a NavPlacement member. Bottom is an unbound NavPlacement.Bottom, a using-static
//   Bottom, a const field followed to an initializer that is one of those, or a cast (or a bare
//   integer) whose constant value is 1. The same forms with Top, or any other known value, stay
//   Top. Any other shape stays Top, with no diagnostic.
//   The enum is emitted internal next to [Page]. Every project that references this generator
//   gets that attribute, and several share a root namespace, so a public enum would collide.
//   The registry stores it on an internal init property for the same reason.
// - AgentActions (typeof list) names the catalog actions the agent may execute while this page is
//   on screen. Types are resolved with the semantic model and emitted fully qualified, so the
//   generated file needs none of the page file's usings. Every page that declares any (navigable
//   or not, parameterized or not) is a PageRegistry.ActionScopes row with all its route templates,
//   so the agent matches the current path to its page without a hand-written route list.
// Incremental caching: PageModel is a value-equatable record (arrays by sequence) holding
// location-free PageDiagnostic values; the Diagnostic is built in RegisterSourceOutput. A held
// Diagnostic/Location pins old SyntaxTrees and never compares equal, which made every edit re-run
// every step — and pages.Collect() would keep all of them alive in one cached array.
// Layout: this class only wires the pipeline. PageCollector builds PageModel (page-model.cs) from
// syntax, PageNavPlacementReader resolves NavPlacement, and PageEmitter renders all source text.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Source generator that emits [Route], navigation helpers, and parameter props for [Page] Blazor pages.</summary>
[Generator]
public sealed class PageSourceGenerator : IIncrementalGenerator
{
  /// <summary>Registers incremental generator steps for [Page] surface emission.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider.Select(
      static (p, _) => Sanitize(p.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? ns) && !string.IsNullOrWhiteSpace(ns)
        ? ns!
        : "Generated"));

    context.RegisterSourceOutput(rootNamespace, static (spc, ns) =>
      spc.AddSource("PageAttribute.g.cs", SourceText.From(PageEmitter.BuildAttribute(ns), Encoding.UTF8)));

    IncrementalValuesProvider<PageModel> pages = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Any(),
      transform: static (ctx, _) => PageCollector.GetPage((ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel))
      .Where(static p => p is not null)
      .Select(static (p, _) => p!);

    context.RegisterSourceOutput(pages.Combine(rootNamespace), static (spc, pair) =>
    {
      PageModel page = pair.Left;
      if (!page.Errors.IsEmpty)
      {
        foreach (PageDiagnostic error in page.Errors)
          spc.ReportDiagnostic(error.ToDiagnostic());
        return;
      }

      if (page.NavigableDiagnostic is not null)
        spc.ReportDiagnostic(page.NavigableDiagnostic.ToDiagnostic());

      spc.AddSource($"{page.HintName}.Page.g.cs", SourceText.From(PageEmitter.Emit(page, pair.Right), Encoding.UTF8));
    });

    context.RegisterSourceOutput(pages.Collect().Combine(rootNamespace), static (spc, pair) =>
    {
      if (pair.Left.IsDefaultOrEmpty) return;
      spc.AddSource("PageRegistry.g.cs", SourceText.From(PageEmitter.EmitRegistry(pair.Left, pair.Right), Encoding.UTF8));
    });
  }

  private static string Sanitize(string ns)
  {
    var sb = new StringBuilder(ns.Length);
    foreach (char c in ns)
      sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' ? c : '_');
    return sb.ToString();
  }
}
