#region Purpose
// TWA0025: Operation-outcome message bars (Error/Success intent) belong in the shell's
// MessageBars.razor host, fed by NotificationState. Pages/cards/features must never render
// their own FluentMessageBar for operation outcomes; static Info/Warning guidance is allowed.
#endregion

#region Design
// Duplicate message bars appeared on the Settings page — multiple components each rendered
// their own Success/Error FluentMessageBar, violating the single-notification-region design.
// This is a direct example of "prefer analyzers/source generators over convention-by-memory"
// (standing directive): the rule cannot be enforced by access modifiers, and convention
// documentation alone was insufficient to prevent the issue.
//
// Scope is ALL razor/cshtml-generated web-spa .cs trees (same as TWA0022): user-authored
// @code in razor components compiles into *_razor.g.cs trees, which Roslyn's path heuristic
// would otherwise classify as generated and exempt from the default GeneratedCodeAnalysisFlags.
// None behavior. Instead, this analyzer uses GeneratedCodeAnalysisFlags.Analyze and applies
// its own path-based exemption: razor/cshtml-generated trees ARE analyzed (their #line pragmas
// map diagnostics back to the .razor source), while other .g.cs trees are exempt. The pattern
// follows TWA0022 precedent (spa-mediator-send-analyzer.cs).
//
// Silence bias (false negatives preferred over false positives): if the Intent value cannot
// be statically resolved to a MessageBarIntent.Error or MessageBarIntent.Success enum member
// (e.g. a ternary, a property reference, or a method call), the analyzer does NOT report.
// A ternary like `IsRequired ? MessageBarIntent.Success : MessageBarIntent.Info` is too complex
// to prove violates the rule, so it is silently approved. This bias prevents false positives
// in edge cases where control flow is too intricate to analyze safely.
//
// Opt-out mechanism: @attribute [PageLocalMessageBar("reason")] in the .razor file, or
// [PageLocalMessageBar("reason")] on the .razor.cs partial class. A non-empty, non-whitespace
// reason string suppresses the diagnostic for the entire type hierarchy (any FluentMessageBar
// in that component, or in a containing type). Empty reason does not opt out — the rule still
// fires; there is no separate diagnostic for that case.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using Microsoft.CodeAnalysis.Operations;

/// <summary>Roslyn analyzer for TWA0025: operation-outcome message bars belong in the shell's MessageBars host.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PageLocalMessageBarAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0025.</summary>
  public const string DiagnosticId = "TWA0025";

  private const string BlazorWasmSdkProperty = "build_property.UsingMicrosoftNETSdkBlazorWebAssembly";
  private const string FluentMessageBarTypeName = "FluentMessageBar";
  private const string MessageBarIntentTypeName = "MessageBarIntent";
  private const string FluentUiNamespace = "Microsoft.FluentUI.AspNetCore.Components";
  private const string OpenComponentMethodName = "OpenComponent";
  private const string CloseComponentMethodName = "CloseComponent";
  private const string AddComponentParameterMethodName = "AddComponentParameter";
  private const string AddAttributeMethodName = "AddAttribute";
  private const string IntentParameterName = "Intent";
  private const string TypeCheckMethodName = "TypeCheck";
  private const string PageLocalMessageBarAttributeName = "PageLocalMessageBarAttribute";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Operation-outcome message bars belong to the shell's MessageBars host",
      messageFormat: "FluentMessageBar with Intent '{0}' outside shell's MessageBars host. Report outcomes through NotificationState so the shell paints them. [PageLocalMessageBar(reason)] on the component opts out.",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Each page has exactly one notification region: the shell's MessageBars.razor component, fed by NotificationState. Pages and feature components must dispatch operation outcomes through NotificationState (AddNotification / ReportProblem from a component, or OutcomeNotification / ProblemDetailsNotification published from a handler), never render their own Error/Success message bars."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  /// <summary>Registers syntax/compilation actions that report TWA0025.</summary>
  public override void Initialize(AnalysisContext context)
  {
    // Deliberately NOT GeneratedCodeAnalysisFlags.None — razor `@code` blocks live in generated
    // trees and are the primary target of this rule. See the Design region.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      if (!IsBlazorWebAssemblyCompilation(startContext.Options)) return;

      startContext.RegisterOperationAction(AnalyzeOpenComponent, OperationKind.Invocation);
    });
  }

  private static bool IsBlazorWebAssemblyCompilation(AnalyzerOptions options) =>
    options.AnalyzerConfigOptionsProvider.GlobalOptions
      .TryGetValue(BlazorWasmSdkProperty, out string? value)
    && string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

  private static void AnalyzeOpenComponent(OperationAnalysisContext context)
  {
    var invocation = (IInvocationOperation)context.Operation;

    // Check if this is an OpenComponent invocation with FluentMessageBar as type argument.
    if (!string.Equals(invocation.TargetMethod.Name, OpenComponentMethodName, StringComparison.Ordinal))
      return;

    if (invocation.TargetMethod.TypeParameters.Length != 1)
      return;

    ITypeSymbol componentType = invocation.TargetMethod.TypeArguments[0];
    if (!IsFluentMessageBarType(componentType))
      return;

    // Exempt non-razor generated code (only analyze razor/@code and ordinary .cs files).
    if (IsExemptGeneratedCode(context))
      return;

    // Exempt components with [PageLocalMessageBar] opt-out attribute.
    if (HasPageLocalMessageBarOptOut(context.ContainingSymbol))
      return;

    // Walk forward through parent block to find Intent parameter.
    IBlockOperation? block = GetEnclosingBlock(invocation);
    if (block is null)
      return;

    // Find this invocation's index by checking which statement contains it
    int openComponentIndex = -1;
    for (int i = 0; i < block.Operations.Length; i++)
    {
      if (OperationContains(block.Operations[i], invocation))
      {
        openComponentIndex = i;
        break;
      }
    }

    if (openComponentIndex < 0)
      return;

    // Scan forward from the OpenComponent statement to CloseComponent.
    for (int i = openComponentIndex + 1; i < block.Operations.Length; i++)
    {
      IOperation operation = block.Operations[i];

      // Extract any invocation from expression statements
      IInvocationOperation? invocationOp = ExtractInvocationFromStatement(operation);
      if (invocationOp is null)
        continue;

      // Stop at CloseComponent.
      if (string.Equals(invocationOp.TargetMethod.Name, CloseComponentMethodName, StringComparison.Ordinal))
      {
        break;
      }

      // Check for AddComponentParameter or AddAttribute with Intent.
      CheckIntentParameter(invocationOp, context);
    }
  }

  private static bool OperationContains(IOperation parent, IOperation child)
  {
    if (parent == child)
      return true;

    foreach (IOperation childOp in parent.ChildOperations)
    {
      if (OperationContains(childOp, child))
        return true;
    }

    return false;
  }

  private static IInvocationOperation? ExtractInvocationFromStatement(IOperation operation)
  {
    if (operation is IInvocationOperation invocation)
      return invocation;

    if (operation is IExpressionStatementOperation exprStmt)
      return exprStmt.Operation as IInvocationOperation;

    return null;
  }

  private static void CheckIntentParameter(IInvocationOperation invocation, OperationAnalysisContext context)
  {
    string methodName = invocation.TargetMethod.Name;
    if (!string.Equals(methodName, AddComponentParameterMethodName, StringComparison.Ordinal)
      && !string.Equals(methodName, AddAttributeMethodName, StringComparison.Ordinal))
    {
      return;
    }

    // Argument 1 (index 1) should be the parameter name.
    if (invocation.Arguments.Length < 3)
      return;

    IArgumentOperation paramNameArg = invocation.Arguments[1];
    string paramName = ExtractConstantString(paramNameArg.Value);
    if (!string.Equals(paramName, IntentParameterName, StringComparison.Ordinal))
      return;

    // Argument 2 (index 2) is the Intent value.
    IArgumentOperation valueArg = invocation.Arguments[2];
    IOperation valueOperation = UnwrapOperation(valueArg.Value);

    // Try to resolve to an Error or Success enum member.
    string? resolvedEnumMember = ResolveEnumMember(valueOperation);
    if (resolvedEnumMember is not null && (resolvedEnumMember == "Error" || resolvedEnumMember == "Success"))
    {
      // Check if this is in the MessageBars host (exempt if so).
      if (IsMessageBarsHostForOperation(valueOperation))
        return;

      context.ReportDiagnostic(Diagnostic.Create(Rule, valueOperation.Syntax.GetLocation(), resolvedEnumMember));
    }
  }

  private static bool IsMessageBarsHostForOperation(IOperation operation)
  {
    string filePath = operation.Syntax.SyntaxTree.FilePath ?? string.Empty;

    // Check the direct file path (normalized).
    if (IsMessageBarsHostPath(filePath))
      return true;

    // Check the #line-mapped path from the syntax location.
    FileLinePositionSpan span = operation.Syntax.GetLocation().GetMappedLineSpan();
    if (span.HasMappedPath)
    {
      if (IsMessageBarsHostPath(span.Path))
        return true;
    }

    return false;
  }

  private static string? ResolveEnumMember(IOperation operation)
  {
    // Case 1: IFieldReferenceOperation (e.g., MessageBarIntent.Error).
    if (operation is IFieldReferenceOperation fieldRef)
    {
      if (IsMessageBarIntentEnum(fieldRef.Field.ContainingType))
      {
        return fieldRef.Field.Name;
      }

      return null;
    }

    // Case 2: Constant value with MessageBarIntent enum type (or nullable variant).
    if (operation.ConstantValue.HasValue && operation.Type is not null)
    {
      ITypeSymbol checkType = operation.Type;
      // Handle nullable MessageBarIntent? — unwrap to underlying type
      if (checkType is INamedTypeSymbol namedType
        && namedType.OriginalDefinition?.Name == "Nullable`1"
        && namedType.TypeArguments.Length == 1)
      {
        checkType = namedType.TypeArguments[0];
      }

      if (IsMessageBarIntentEnum(checkType))
      {
        // Map the constant integer to enum member name.
        object? constVal = operation.ConstantValue.Value;
        if (constVal is int enumInt)
        {
          return GetMessageBarIntentMemberName(enumInt);
        }
      }
    }

    return null;
  }

  private static string? GetMessageBarIntentMemberName(int value) =>
    value switch
    {
      0 => "Info",
      1 => "Warning",
      2 => "Error",
      3 => "Success",
      _ => null
    };

  private static bool IsExemptGeneratedCode(OperationAnalysisContext context)
  {
    string path = context.Operation.Syntax.SyntaxTree.FilePath ?? string.Empty;

    // Razor/cshtml-generated trees carry user-authored @code — analyze them.
    if (IsRazorGeneratedPath(path))
      return false;

    // Other generated trees are exempt.
    if (IsGeneratedPath(path))
      return true;

    return false;
  }

  private static bool IsRazorGeneratedPath(string path) =>
    path.EndsWith("_razor.g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".razor.g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith("_cshtml.g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".cshtml.g.cs", StringComparison.OrdinalIgnoreCase);

  private static bool IsGeneratedPath(string path) =>
    path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
    || path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);

  private static IOperation UnwrapOperation(IOperation operation)
  {
    while (operation is IConversionOperation conversion)
    {
      operation = conversion.Operand;
    }

    while (operation is IParenthesizedOperation paren)
    {
      operation = paren.Operand;
    }

    // Unwrap TypeCheck calls.
    if (operation is IInvocationOperation invocation
      && string.Equals(invocation.TargetMethod.Name, TypeCheckMethodName, StringComparison.Ordinal)
      && invocation.Arguments.Length > 0)
    {
      operation = UnwrapOperation(invocation.Arguments[0].Value);
    }

    return operation;
  }

  private static bool IsFluentMessageBarType(ITypeSymbol type)
  {
    return type.Name == FluentMessageBarTypeName
      && type.ContainingNamespace.ToDisplayString() == FluentUiNamespace;
  }

  private static bool IsMessageBarIntentEnum(ITypeSymbol? type)
  {
    return type?.Name == MessageBarIntentTypeName
      && type?.ContainingNamespace.ToDisplayString() == FluentUiNamespace;
  }

  private static string ExtractConstantString(IOperation operation)
  {
    if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is string str)
    {
      return str;
    }

    return string.Empty;
  }

  private static IBlockOperation? GetEnclosingBlock(IInvocationOperation invocation)
  {
    IOperation? current = invocation;
    while (current is not null)
    {
      if (current is IBlockOperation block)
      {
        return block;
      }

      current = current.Parent;
    }

    return null;
  }

  private static bool IsMessageBarsHostPath(string path)
  {
    // Normalize backslashes to forward slashes.
    path = path.Replace("\\", "/", StringComparison.Ordinal);

    // Case-insensitive file name check.
    string fileName = Path.GetFileName(path);
    if (string.Equals(fileName, "MessageBars.razor", StringComparison.OrdinalIgnoreCase))
      return true;

    // Check for generated-tree suffixes (components_MessageBars_razor.g.cs, etc.).
    if (path.Contains("MessageBars_razor", StringComparison.OrdinalIgnoreCase)
      || path.Contains("MessageBars.razor", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    return false;
  }

  private static bool HasPageLocalMessageBarOptOut(ISymbol? symbol)
  {
    // Walk up through containing types.
    for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
    {
      if (current is INamespaceSymbol)
        break;

      if (current is INamedTypeSymbol namedType)
      {
        if (HasOptOutAttribute(namedType))
          return true;
      }
    }

    return false;
  }

  private static bool HasOptOutAttribute(INamedTypeSymbol type)
  {
    foreach (AttributeData attribute in type.GetAttributes())
    {
      // Match by simple name to avoid ProjectReference dependency.
      if (attribute.AttributeClass?.Name == PageLocalMessageBarAttributeName)
      {
        // Check if reason is non-empty.
        if (attribute.ConstructorArguments.Length > 0)
        {
          TypedConstant reasonArg = attribute.ConstructorArguments[0];
          if (reasonArg.Value is string reason && !string.IsNullOrWhiteSpace(reason))
          {
            return true;
          }
        }
      }
    }

    return false;
  }
}
