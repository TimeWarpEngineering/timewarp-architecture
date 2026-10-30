#region Purpose
// TWA0036: every date or time rendered as text is ISO 8601. Flags culture-shaped renderings of
// DateTime, DateTimeOffset, DateOnly, and TimeOnly.
#endregion

#region Design
// Flagged shapes (each on a DateTime / DateTimeOffset / DateOnly / TimeOnly value, nullable or not):
//   - ToShortDateString / ToLongDateString / ToShortTimeString / ToLongTimeString.
//   - ToString with no format (ToString(), ToString(IFormatProvider), Nullable<T>.ToString()):
//     the culture's "G" shape.
//   - ToString with a constant format that is not an approved ISO shape. A non-constant format
//     is not flagged; the analyzer cannot see it.
//   - ToString with an approved custom format and no CultureInfo.InvariantCulture (or
//     DateTimeFormatInfo.InvariantInfo) provider. Custom formats follow the provider: `:` renders
//     the culture's TimeSeparator and `yyyy` its calendar (th-TH gives 2569). Only "O", "o", and
//     "s" are invariant on their own. WASM forces en-US, but the server renders the same razor
//     under the host culture.
//   - An interpolation hole of a date type with no format or a non-ISO one, or with an approved
//     custom format outside an invariant call. Invariant calls are FormattableString.Invariant and
//     a handler-taking call given the invariant provider (string.Create(CultureInfo.InvariantCulture,
//     $"…"), StringBuilder.Append(provider, $"…")): the interpolated string binds to a handler whose
//     constructor takes that call's IFormatProvider argument. An interpolated string passed as a
//     plain string, object, or FormattableString parameter is built under the current culture
//     before the callee sees the provider, so Foo(CultureInfo.InvariantCulture, $"…") and
//     string.Format(CultureInfo.InvariantCulture, $"…") still flag.
//     The interpolated string must be that call's argument directly. Both hole shapes are
//     covered: an interpolated string converted to string, object, or FormattableString binds
//     as IInterpolationOperation (the DefaultInterpolatedStringHandler rewrite happens in
//     lowering, which IOperation does not see); a bind-time handler conversion (string.Create,
//     StringBuilder.Append) binds AppendFormatted invocations. Only one shape exists per hole, so
//     no double report.
//   - String concatenation of a date (`"on " + date`, `text += date`): implicit ToString().
//   - Razor `@date` content: RenderTreeBuilder.AddContent(int, object) with a boxed date.
//     Attribute values are not flagged, because component parameters share that path.
// Not covered: string.Format / StringBuilder.Append / Console.Write of a date. Those are rare
// render paths in this template; `#pragma warning disable TWA0036` is the valve for a real
// exception.
//
// Approved formats: standard "O", "o", "s"; and custom formats whose shape is ISO 8601 extended
// (yyyy, yyyy-MM, yyyy-MM-dd, yyyy-MM-ddTHH:mm[:ss[.f…]][Z|zzz|K], HH:mm[:ss[.f…]][Z|zzz|K]) or
// basic (yyyyMMdd[THHmm[ss[.f…]]][Z|zzz|K]). Quote and backslash escapes are stripped before the
// match, so `yyyy-MM-dd'T'HH:mm:ss'Z'` and `yyyy-MM-ddTHH:mm:ssZ` both pass. "u" is NOT approved:
// it separates date and time with a space. "R" (RFC 1123) and every single-letter culture
// specifier ("d", "t", "g", ...) are not ISO.
//
// Scope is every compilation the convention analyzers attach to, test projects included, so a
// test that pins a rendered date is written in the same shape the app renders. Generated-code
// handling matches TWA0022: razor/cshtml-generated trees ARE analyzed (component markup and
// @code live there), other generated trees (EF migrations, protobuf) are exempt.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Operations;

/// <summary>Roslyn analyzer for TWA0036: dates and times render as ISO 8601.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class IsoDateRenderingAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0036.</summary>
  public const string DiagnosticId = "TWA0036";

  private const string GeneratedCodeAttributeFullName = "System.CodeDom.Compiler.GeneratedCodeAttribute";
  private const string InterpolatedStringHandlerAttributeFullName = "System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute";
  private const string RenderTreeBuilderFullName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
  private const string WithoutInvariant = " without CultureInfo.InvariantCulture";

  private static readonly string[] DateTypeNames =
    ["System.DateTime", "System.DateTimeOffset", "System.DateOnly", "System.TimeOnly"];

  private static readonly ImmutableHashSet<string> CultureShortcutMethods =
    ImmutableHashSet.Create
    (
      StringComparer.Ordinal,
      "ToShortDateString",
      "ToLongDateString",
      "ToShortTimeString",
      "ToLongTimeString"
    );

  private static readonly ImmutableHashSet<string> ApprovedStandardFormats =
    ImmutableHashSet.Create(StringComparer.Ordinal, "O", "o", "s");

  [GeneratedRegex(@"^yyyy(-MM(-dd(THH:mm(:ss(\.(f{1,7}|F{1,7}))?)?(Z|zzz|K)?)?)?)?$", RegexOptions.CultureInvariant)]
  private static partial Regex ExtendedDateShape();

  [GeneratedRegex(@"^HH:mm(:ss(\.(f{1,7}|F{1,7}))?)?(Z|zzz|K)?$", RegexOptions.CultureInvariant)]
  private static partial Regex ExtendedTimeShape();

  [GeneratedRegex(@"^yyyyMMdd(THHmm(ss(\.(f{1,7}|F{1,7}))?)?(Z|zzz|K)?)?$", RegexOptions.CultureInvariant)]
  private static partial Regex BasicShape();

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Dates and times render as ISO 8601",
      messageFormat: "{0} does not render ISO 8601; use an ISO 8601 format (yyyy-MM-dd for a date, yyyy-MM-ddTHH:mm:sszzz for an instant) with CultureInfo.InvariantCulture",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Every user-visible date or time is ISO 8601. Culture shortcuts (ToShortDateString and friends), format-less ToString, string concatenation, and non-ISO format strings render in whatever shape the current culture or the author chose. A custom format also follows the culture's separators and calendar unless the provider is CultureInfo.InvariantCulture."
    );

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  /// <summary>Registers operation actions that report TWA0036.</summary>
  public override void Initialize(AnalysisContext context)
  {
    // Razor markup and @code compile into generated trees; see the Design region.
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      var dateTypes =
        DateTypeNames
          .Select(startContext.Compilation.GetTypeByMetadataName)
          .Where(type => type is not null)
          .Select(type => type!)
          .ToImmutableArray();
      if (dateTypes.IsEmpty) return;

      startContext.RegisterOperationAction(c => AnalyzeInvocation(c, dateTypes), OperationKind.Invocation);
      startContext.RegisterOperationAction(c => AnalyzeInterpolation(c, dateTypes), OperationKind.Interpolation);
      startContext.RegisterOperationAction(c => AnalyzeBinary(c, dateTypes), OperationKind.Binary);
      startContext.RegisterOperationAction(c => AnalyzeCompoundAssignment(c, dateTypes), OperationKind.CompoundAssignment);
    });
  }

  private static void AnalyzeInvocation(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    var invocation = (IInvocationOperation)context.Operation;
    IMethodSymbol method = invocation.TargetMethod;

    string? description = method.Name switch
    {
      "ToString" => DescribeToString(invocation, dateTypes),
      "AppendFormatted" => DescribeAppendFormatted(invocation, dateTypes),
      "AddContent" => DescribeAddContent(invocation, dateTypes),
      _ when CultureShortcutMethods.Contains(method.Name)
        && IsDateType(invocation.Instance?.Type ?? method.ContainingType, dateTypes) => method.Name + "()",
      _ => null
    };

    if (description is null || IsExemptGeneratedCode(context)) return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), description));
  }

  private static string? DescribeToString(IInvocationOperation invocation, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    ITypeSymbol? receiverType = invocation.Instance?.Type;
    if (!IsDateType(receiverType, dateTypes)) return null;

    IArgumentOperation? format = FormatArgument(invocation);
    if (format is null) return "ToString() without a format";

    if (NonIsoConstantFormat(format.Value) is string text) return $"ToString(\"{text}\")";

    return NeedsInvariantProvider(format.Value) && !HasInvariantProviderArgument(invocation)
      ? $"ToString(\"{format.Value.ConstantValue.Value}\"){WithoutInvariant}"
      : null;
  }

  private static string? DescribeAppendFormatted(IInvocationOperation invocation, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    if (!IsInterpolatedStringHandler(invocation.TargetMethod.ContainingType)) return null;
    if (invocation.Arguments.Length == 0) return null;

    IOperation value = Unwrap(invocation.Arguments[0].Value);
    if (!IsDateType(value.Type, dateTypes)) return null;

    IArgumentOperation? format = FormatArgument(invocation);
    if (format is null || format.ArgumentKind == ArgumentKind.DefaultValue) return "An interpolation hole without a format";

    return DescribeHoleFormat(format.Value, invocation);
  }

  private static string? DescribeAddContent(IInvocationOperation invocation, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    if (invocation.TargetMethod.ContainingType?.ToDisplayString() != RenderTreeBuilderFullName) return null;

    IArgumentOperation? content =
      invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Type.SpecialType == SpecialType.System_Object);
    if (content is null) return null;

    return IsDateType(Unwrap(content.Value).Type, dateTypes) ? "Razor content of a date value" : null;
  }

  private static void AnalyzeInterpolation(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    var interpolation = (IInterpolationOperation)context.Operation;
    if (!IsDateType(Unwrap(interpolation.Expression).Type, dateTypes)) return;

    string? description =
      interpolation.FormatString is null
        ? "An interpolation hole without a format"
        : DescribeHoleFormat(interpolation.FormatString, interpolation);

    if (description is null || IsExemptGeneratedCode(context)) return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, interpolation.Syntax.GetLocation(), description));
  }

  private static void AnalyzeBinary(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    var binary = (IBinaryOperation)context.Operation;
    if (binary.OperatorKind != BinaryOperatorKind.Add) return;
    if (binary.Type?.SpecialType != SpecialType.System_String) return;

    IOperation? dateOperand =
      new[] { binary.LeftOperand, binary.RightOperand }
        .FirstOrDefault(operand => IsDateType(Unwrap(operand).Type, dateTypes));
    if (dateOperand is null || IsExemptGeneratedCode(context)) return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, dateOperand.Syntax.GetLocation(), "String concatenation of a date"));
  }

  private static void AnalyzeCompoundAssignment(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    var assignment = (ICompoundAssignmentOperation)context.Operation;
    if (assignment.OperatorKind != BinaryOperatorKind.Add) return;
    if (assignment.Target.Type?.SpecialType != SpecialType.System_String) return;
    if (!IsDateType(Unwrap(assignment.Value).Type, dateTypes) || IsExemptGeneratedCode(context)) return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.Value.Syntax.GetLocation(), "String concatenation of a date"));
  }

  private static string? DescribeHoleFormat(IOperation format, IOperation hole)
  {
    if (NonIsoConstantFormat(format) is string text) return $"Interpolation format \":{text}\"";

    return NeedsInvariantProvider(format) && !IsInInvariantInterpolation(hole)
      ? $"Interpolation format \":{format.ConstantValue.Value}\"{WithoutInvariant}"
      : null;
  }

  /// <summary>True when the format is an approved custom shape, which follows the provider's culture.</summary>
  private static bool NeedsInvariantProvider(IOperation format) =>
    format.ConstantValue is { HasValue: true, Value: string text } && !ApprovedStandardFormats.Contains(text);

  private static bool HasInvariantProviderArgument(IInvocationOperation invocation) =>
    invocation.Arguments.Any
    (
      argument =>
        IsFormatProviderType(argument.Parameter?.Type) && IsInvariantProvider(argument.Value)
    );

  private static bool IsFormatProviderType(ITypeSymbol? type) =>
    type is INamedTypeSymbol { Name: "IFormatProvider" } providerType
    && providerType.ContainingNamespace?.ToDisplayString() == "System";

  private static bool IsInvariantProvider(IOperation provider) =>
    Unwrap(provider) is IPropertyReferenceOperation { Property: { IsStatic: true } property }
    && (property.Name, property.ContainingType?.ToDisplayString()) is
      ("InvariantCulture", "System.Globalization.CultureInfo")
      or ("InvariantInfo", "System.Globalization.DateTimeFormatInfo");

  /// <summary>
  /// True when the hole's interpolated string is the direct argument of FormattableString.Invariant,
  /// or binds to a handler that takes the invariant provider the call passes.
  /// </summary>
  private static bool IsInInvariantInterpolation(IOperation hole)
  {
    IOperation? current = hole.Parent;
    while (current is not null and not IInterpolatedStringOperation)
    {
      current = current.Parent;
    }

    current = current?.Parent;
    IInterpolatedStringHandlerCreationOperation? handler = null;
    while (current is IConversionOperation or IInterpolatedStringHandlerCreationOperation)
    {
      handler ??= current as IInterpolatedStringHandlerCreationOperation;
      current = current.Parent;
    }

    if (current is not IArgumentOperation { Parent: IInvocationOperation call }) return false;

    IMethodSymbol method = call.TargetMethod;
    bool formattableInvariant =
      method.Name == "Invariant" && method.ContainingType?.ToDisplayString() == "System.FormattableString";
    return formattableInvariant
      || (handler is not null && HandlerTakesCallProvider(handler) && HasInvariantProviderArgument(call));
  }

  /// <summary>
  /// True when the handler's constructor receives an IFormatProvider from the enclosing call's
  /// arguments ([InterpolatedStringHandlerArgument("provider")]), so the holes format under it.
  /// </summary>
  private static bool HandlerTakesCallProvider(IInterpolatedStringHandlerCreationOperation handler) =>
    handler.HandlerCreation is IObjectCreationOperation creation
    && creation.Arguments.Any
    (
      argument =>
        IsFormatProviderType(argument.Parameter?.Type)
        && Unwrap(argument.Value) is IInterpolatedStringHandlerArgumentPlaceholderOperation
        {
          PlaceholderKind: InterpolatedStringArgumentPlaceholderKind.CallsiteArgument,
        }
    );

  private static IArgumentOperation? FormatArgument(IInvocationOperation invocation) =>
    invocation.Arguments.FirstOrDefault
    (
      argument =>
        argument.Parameter?.Type.SpecialType == SpecialType.System_String
        && string.Equals(argument.Parameter.Name, "format", StringComparison.Ordinal)
    );

  /// <summary>The format text when it is a constant that is not an approved ISO shape; otherwise null.</summary>
  private static string? NonIsoConstantFormat(IOperation format)
  {
    Optional<object?> constant = format.ConstantValue;
    if (!constant.HasValue) return null;

    string text = constant.Value as string ?? string.Empty;
    return IsApprovedFormat(text) ? null : text;
  }

  /// <summary>True when <paramref name="format"/> renders an ISO 8601 shape.</summary>
  internal static bool IsApprovedFormat(string format)
  {
    if (ApprovedStandardFormats.Contains(format)) return true;

    string shape = format
        .Replace("'", string.Empty, StringComparison.Ordinal)
        .Replace("\"", string.Empty, StringComparison.Ordinal)
        .Replace("\\", string.Empty, StringComparison.Ordinal);
    if (shape.Length <= 1) return false;

    return ExtendedDateShape().IsMatch(shape) || ExtendedTimeShape().IsMatch(shape) || BasicShape().IsMatch(shape);
  }

  private static IOperation Unwrap(IOperation operation)
  {
    while (operation is IConversionOperation { IsImplicit: true } conversion)
    {
      operation = conversion.Operand;
    }

    return operation;
  }

  private static bool IsDateType(ITypeSymbol? type, ImmutableArray<INamedTypeSymbol> dateTypes)
  {
    if (type is null) return false;

    if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
    {
      type = nullable.TypeArguments[0];
    }

    return dateTypes.Any(dateType => SymbolEqualityComparer.Default.Equals(type, dateType));
  }

  private static bool IsInterpolatedStringHandler(INamedTypeSymbol? type) =>
    type?.GetAttributes().Any(attribute =>
      attribute.AttributeClass?.ToDisplayString() == InterpolatedStringHandlerAttributeFullName) == true;

  private static bool IsExemptGeneratedCode(OperationAnalysisContext context)
  {
    string path = context.Operation.Syntax.SyntaxTree.FilePath ?? string.Empty;

    // Razor/cshtml-generated trees carry component markup and @code — analyze them.
    if (IsRazorGeneratedPath(path)) return false;

    if (IsGeneratedPath(path)) return true;

    return HasGeneratedCodeAttribute(context.ContainingSymbol);
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

  private static bool HasGeneratedCodeAttribute(ISymbol? symbol)
  {
    for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
    {
      if (current is INamespaceSymbol) break;

      if (current.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == GeneratedCodeAttributeFullName))
      {
        return true;
      }
    }

    return false;
  }
}
