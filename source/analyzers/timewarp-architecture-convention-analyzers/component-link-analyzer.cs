#region Purpose
// TWA0050: a link written in component markup (Href / href / To, NavLink, <a href>, Fluent anchors)
// navigates without a TimeWarp.State action. The component dispatches RouteState.ChangeRoute (or
// the page's navigate action) instead, or opts out with [DirectComponentSideEffect(reason)].
#endregion

#region Design
// Task 506 closes the markup gap in TWA0026 (component-side-effect-analyzer.cs). TWA0026 matches
// method calls, so `Href="…"` never reached it: Razor compiles markup into RenderTreeBuilder calls
// whose attribute names are strings. This analyzer reads those calls.
//
// What is read, inside a ComponentBase-derived type (the nearest containing named type, as TWA0026):
//   - AddComponentParameter / AddAttribute named `href` (any case) or `To` under an OpenComponent<T>:
//     FluentAnchorButton, FluentAnchor, FluentLink, FluentButton Href, FluentNavItem, NavLink href.
//   - AddAttribute named `href` under OpenElement "a". Other elements are not links (`<link href>`,
//     `<base href>`, SVG `<use href>`), so they are not read.
//   - AddMarkupContent: Razor folds a fully static subtree into one markup string, so a static
//     `<a href="/">` never becomes an AddAttribute call. Each `<a …>` tag in the string is parsed.
// The owning element is found by walking the statements of the enclosing block back to the
// nearest Open* call: Razor emits an element's attributes as consecutive statements right after
// its Open*, before any child frame. A call whose opener cannot be found is not reported
// (silence bias, as TWA0025).
// Known gaps: `@attributes` splats (AddMultipleAttributes) are not read, and a generic component
// whose parameters go through Razor's TypeInference helper class is outside any component type.
//
// Value rules. A constant null (or false) value removes the attribute, so it is not a link. An
// external link is not reported: a constant href with an http:, https:, mailto: or tel: scheme
// that opens a new tab (target "_blank", or a `Target` enum member named Blank) or says
// rel="external". A non-constant href is external only with rel="external": the author states the
// link leaves the app, and a new tab alone does not say where the href points.
//
// Opt-out and gating reuse TWA0026: [DirectComponentSideEffect("reason")] on the component, an
// enclosing type, or the member that builds the markup; TWA0027 already reports an empty reason.
// SPA code only (UsingMicrosoftNETSdkBlazorWebAssembly); razor-generated trees are analyzed.
// The diagnostic sits on the attribute value, which Razor's #line pragmas map to the .razor line;
// a literal value sits on a hidden line, so that diagnostic points into the generated file.
// Severity is Error: a link that bypasses the store is invisible to Ask AI's action catalog.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;

/// <summary>Roslyn analyzer for TWA0050: component links are State navigate actions.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class ComponentLinkAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0050.</summary>
  public const string DiagnosticId = "TWA0050";

  private const string ComponentBaseFullName = "Microsoft.AspNetCore.Components.ComponentBase";
  private const string RenderTreeBuilderFullName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
  private const string AddAttributeName = "AddAttribute";
  private const string AddComponentParameterName = "AddComponentParameter";
  private const string AddMarkupContentName = "AddMarkupContent";
  private const string OpenElementName = "OpenElement";
  private const string OpenComponentName = "OpenComponent";

  // Calls Razor emits between an Open* and the element's first child frame.
  private static readonly ImmutableHashSet<string> AttributeFrameMethods =
    ImmutableHashSet.Create
    (
      StringComparer.Ordinal,
      AddAttributeName,
      AddComponentParameterName,
      "AddMultipleAttributes",
      "SetKey",
      "AddElementReferenceCapture",
      "AddComponentReferenceCapture",
      "AddEventPreventDefaultAttribute",
      "AddEventStopPropagationAttribute",
      "SetUpdatesAttributeName",
      "AddComponentRenderMode",
      "AddNamedEvent"
    );

  private static readonly ImmutableArray<string> ExternalSchemes =
    ImmutableArray.Create("http://", "https://", "mailto:", "tel:");

  [GeneratedRegex(@"<a(?=[\s/>])(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex AnchorTag();

  [GeneratedRegex(@"(?<name>[^\s=/>""']+)(?:\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s""'=<>`]+)))?", RegexOptions.CultureInvariant)]
  private static partial Regex MarkupAttribute();

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Component links are State navigate actions",
      messageFormat: "{0} navigates without a TimeWarp.State action; dispatch RouteState.ChangeRoute (or the page's navigate action) from a State action, as RouteLink does, or opt out with [DirectComponentSideEffect(reason)]",
      category: "Design",
      DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Every SPA user interaction is a TimeWarp.State action. A Href, NavLink, or <a href> in component markup navigates outside the store, so the action catalog and Ask AI cannot see it. External links that open a new tab or say rel=\"external\" are not reported."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  /// <summary>Registers the RenderTreeBuilder invocation action that reports TWA0050.</summary>
  public override void Initialize(AnalysisContext context)
  {
    // Razor markup compiles into generated trees, the only place this rule looks. See Design.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      if (!ComponentSideEffectAnalyzer.IsBlazorWebAssemblyCompilation(startContext.Options)) return;

      INamedTypeSymbol? componentBase = startContext.Compilation.GetTypeByMetadataName(ComponentBaseFullName);
      if (componentBase is null) return;

      startContext.RegisterOperationAction
      (
        operationContext => Analyze(operationContext, componentBase),
        OperationKind.Invocation
      );
    });
  }

  private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol componentBase)
  {
    var invocation = (IInvocationOperation)context.Operation;
    string name = invocation.TargetMethod.Name;
    if (name is not (AddAttributeName or AddComponentParameterName or AddMarkupContentName)) return;
    if (!IsRenderTreeBuilder(invocation.TargetMethod)) return;
    if (!ComponentSideEffectAnalyzer.IsInComponent(context.ContainingSymbol, componentBase)) return;
    if (ComponentSideEffectAnalyzer.IsExemptGeneratedCode(context)) return;
    if (ComponentSideEffectAnalyzer.HasOptOut(context.ContainingSymbol)) return;

    if (name == AddMarkupContentName)
    {
      AnalyzeMarkup(context, invocation);
      return;
    }

    AnalyzeAttribute(context, invocation);
  }

  private static bool IsRenderTreeBuilder(IMethodSymbol method) =>
    method.ContainingType?.ToDisplayString() == RenderTreeBuilderFullName;

  private static void AnalyzeAttribute(OperationAnalysisContext context, IInvocationOperation invocation)
  {
    if (ConstantString(ArgumentAt(invocation, 1)) is not { } attributeName) return;
    if (!IsLinkAttributeName(attributeName)) return;

    IOperation? rawValue = ArgumentAt(invocation, 2);
    if (rawValue is null) return;
    IOperation value = Unwrap(rawValue);
    if (IsRemovedValue(value)) return;

    if (FindElement(invocation) is not { } element) return;

    string subject;
    if (element.ComponentName is { } componentName)
    {
      subject = $"{componentName} {attributeName}";
    }
    else
    {
      if (!string.Equals(element.ElementName, "a", StringComparison.OrdinalIgnoreCase)) return;
      if (!string.Equals(attributeName, "href", StringComparison.OrdinalIgnoreCase)) return;
      subject = "<a href>";
    }

    string? href = value.ConstantValue.HasValue ? value.ConstantValue.Value as string : null;
    if (IsExternal(href, IsNewTab(element.Attributes), IsRelExternal(element.Attributes))) return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation(), subject));
  }

  private static void AnalyzeMarkup(OperationAnalysisContext context, IInvocationOperation invocation)
  {
    IOperation? markupArgument = ArgumentAt(invocation, 1);
    if (ConstantString(markupArgument) is not { } markup) return;

    foreach (Match tag in AnchorTag().Matches(markup))
    {
      string? href = null;
      bool hasHref = false;
      bool newTab = false;
      bool relExternal = false;
      foreach (Match attribute in MarkupAttribute().Matches(tag.Groups["attributes"].Value))
      {
        string attributeName = attribute.Groups["name"].Value;
        string attributeValue = attribute.Groups["value"].Value;
        if (string.Equals(attributeName, "href", StringComparison.OrdinalIgnoreCase))
        {
          hasHref = true;
          href = attributeValue;
        }
        else if (string.Equals(attributeName, "target", StringComparison.OrdinalIgnoreCase))
        {
          newTab = IsBlankTarget(attributeValue);
        }
        else if (string.Equals(attributeName, "rel", StringComparison.OrdinalIgnoreCase))
        {
          relExternal = HasExternalRel(attributeValue);
        }
      }

      if (!hasHref || IsExternal(href, newTab, relExternal)) continue;

      context.ReportDiagnostic(Diagnostic.Create(Rule, markupArgument!.Syntax.GetLocation(), "<a href>"));
      return;
    }
  }

  private static bool IsLinkAttributeName(string name) =>
    string.Equals(name, "href", StringComparison.OrdinalIgnoreCase)
    || string.Equals(name, "To", StringComparison.Ordinal);

  private static bool IsExternal(string? constantHref, bool newTab, bool relExternal)
  {
    if (constantHref is null) return relExternal;

    string trimmed = constantHref.Trim();
    bool externalScheme = ExternalSchemes.Any(scheme => trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase));
    return externalScheme && (newTab || relExternal);
  }

  private static bool IsNewTab(ImmutableArray<(string Name, IOperation Value)> attributes) =>
    attributes.Any(attribute =>
      string.Equals(attribute.Name, "target", StringComparison.OrdinalIgnoreCase)
      && (IsBlankTarget(ConstantString(attribute.Value)) || IsBlankEnumMember(attribute.Value)));

  private static bool IsRelExternal(ImmutableArray<(string Name, IOperation Value)> attributes) =>
    attributes.Any(attribute =>
      string.Equals(attribute.Name, "rel", StringComparison.OrdinalIgnoreCase)
      && HasExternalRel(ConstantString(attribute.Value)));

  private static bool IsBlankTarget(string? target) =>
    string.Equals(target?.Trim(), "_blank", StringComparison.OrdinalIgnoreCase);

  private static bool IsBlankEnumMember(IOperation value) =>
    Unwrap(value) is IFieldReferenceOperation { Field: { Name: "Blank", ContainingType.TypeKind: TypeKind.Enum } };

  private static bool HasExternalRel(string? rel) =>
    rel?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
      .Any(token => string.Equals(token, "external", StringComparison.OrdinalIgnoreCase)) == true;

  private static bool IsRemovedValue(IOperation value) =>
    value.ConstantValue.HasValue && (value.ConstantValue.Value is null or false);

  private static IOperation? ArgumentAt(IInvocationOperation invocation, int ordinal)
  {
    foreach (IArgumentOperation argument in invocation.Arguments)
    {
      if (argument.Parameter?.Ordinal == ordinal) return argument.Value;
    }

    return null;
  }

  private static string? ConstantString(IOperation? operation)
  {
    if (operation is null) return null;
    IOperation unwrapped = Unwrap(operation);
    return unwrapped.ConstantValue.HasValue ? unwrapped.ConstantValue.Value as string : null;
  }

  // Strips the casts, parentheses and RuntimeHelpers.TypeCheck<T>(value) Razor wraps around values.
  private static IOperation Unwrap(IOperation operation)
  {
    IOperation current = operation;
    while (true)
    {
      switch (current)
      {
        case IConversionOperation conversion:
          current = conversion.Operand;
          continue;
        case IParenthesizedOperation parenthesized:
          current = parenthesized.Operand;
          continue;
        case IInvocationOperation { TargetMethod.Name: "TypeCheck", Arguments.Length: 1 } typeCheck:
          current = typeCheck.Arguments[0].Value;
          continue;
        default:
          return current;
      }
    }
  }

  private sealed class Element
  {
    public Element(string? elementName, string? componentName, ImmutableArray<(string Name, IOperation Value)> attributes)
    {
      ElementName = elementName;
      ComponentName = componentName;
      Attributes = attributes;
    }

    public string? ElementName { get; }
    public string? ComponentName { get; }
    public ImmutableArray<(string Name, IOperation Value)> Attributes { get; }
  }

  private static Element? FindElement(IInvocationOperation invocation)
  {
    if (invocation.Parent is not IExpressionStatementOperation statement) return null;
    if (statement.Parent is not IBlockOperation block) return null;

    int index = block.Operations.IndexOf(statement);
    if (index < 0) return null;

    int openIndex = FindOpenerIndex(block, index);
    if (openIndex < 0) return null;
    IInvocationOperation opener = BuilderCall(block.Operations[openIndex])!;

    ImmutableArray<(string Name, IOperation Value)>.Builder attributes = ImmutableArray.CreateBuilder<(string, IOperation)>();
    for (int i = openIndex + 1; i < block.Operations.Length; i++)
    {
      if (BuilderCall(block.Operations[i]) is not { } call) break;
      if (!AttributeFrameMethods.Contains(call.TargetMethod.Name)) break;
      if (ConstantString(ArgumentAt(call, 1)) is { } attributeName && ArgumentAt(call, 2) is { } attributeValue)
      {
        attributes.Add((attributeName, attributeValue));
      }
    }

    if (opener.TargetMethod.Name == OpenElementName)
    {
      return new Element(ConstantString(ArgumentAt(opener, 1)), componentName: null, attributes.ToImmutable());
    }

    string componentName = opener.TargetMethod.TypeArguments.Length == 1
      ? opener.TargetMethod.TypeArguments[0].Name
      : "Component";
    return new Element(elementName: null, componentName, attributes.ToImmutable());
  }

  // Index of the Open* call that owns the attribute statement at attributeIndex, or -1.
  private static int FindOpenerIndex(IBlockOperation block, int attributeIndex)
  {
    for (int i = attributeIndex - 1; i >= 0; i--)
    {
      if (BuilderCall(block.Operations[i]) is not { } call) return -1;
      string name = call.TargetMethod.Name;
      if (name is OpenElementName or OpenComponentName) return i;
      if (!AttributeFrameMethods.Contains(name)) return -1;
    }

    return -1;
  }

  private static IInvocationOperation? BuilderCall(IOperation operation) =>
    operation is IExpressionStatementOperation { Operation: IInvocationOperation call } && IsRenderTreeBuilder(call.TargetMethod)
      ? call
      : null;
}
