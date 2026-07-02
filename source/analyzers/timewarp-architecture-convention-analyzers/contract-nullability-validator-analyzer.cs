#region Purpose
// TWPA0002/TWPA0003: a contract property's nullability annotation and its FluentValidation
// presence rule must agree.
#endregion

#region Design
// Full rationale (the two contradictions, deliberately skipped shapes) lives in the XML doc on
// the class — reconcile it with any detection change.
// Reference types only: value-type presence semantics do not produce the same contradiction.
// Ships in the contract-analyzers assembly (DiagnosticAnalyzers only, no generators) so contract
// projects can adopt the check without triggering FastEndpoint generation.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>
/// Flags contradictions between a property's declared nullability and a FluentValidation
/// presence rule (<c>NotEmpty()</c> / <c>NotNull()</c>) applied to it.
///
/// The rule is NOT "properties must be non-nullable" — <c>string?</c> is legitimate for a
/// genuinely optional field. The defect is the *contradiction*:
///   TWPA0002 (A) — the property is declared nullable (<c>string?</c>) yet a presence rule
///                  says it must be present. Make it non-nullable, or drop the rule if optional.
///   TWPA0003 (B) — the property is non-nullable but initialized with <c>= string.Empty</c> /
///                  <c>= ""</c>, a fabricated valid-looking default that hides an unset field
///                  from the presence rule. Use <c>= null!</c> or <c>required</c>.
///
/// Detection is direct: <c>RuleFor(x => x.Prop)...NotEmpty()/NotNull()</c> inside an
/// <c>AbstractValidator&lt;T&gt;</c>, checked against <c>T.Prop</c>. Whole-object rules
/// (<c>RuleFor(x => x)</c>, e.g. <c>SetValidator</c> composition) and non-trivial lambda bodies
/// (<c>x => x.Prop.Trim()</c>) are intentionally skipped to avoid false positives.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ContractNullabilityValidatorAnalyzer : DiagnosticAnalyzer
{
  public const string NullableWithPresenceRuleId = "TWPA0002";
  public const string EmptyDefaultWithPresenceRuleId = "TWPA0003";

  private const string Category = "Design";

  private static readonly DiagnosticDescriptor NullableWithPresenceRule =
    new
    (
      NullableWithPresenceRuleId,
      title: "Nullable property has a presence validation rule",
      messageFormat: "Property '{0}' is declared nullable but a NotEmpty()/NotNull() rule requires it; make it non-nullable (or drop the rule if it is optional)",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A property with an unconditional NotEmpty()/NotNull() rule is required, so its type must not be nullable. The type annotation is the contract; the validator must agree."
    );

  private static readonly DiagnosticDescriptor EmptyDefaultWithPresenceRule =
    new
    (
      EmptyDefaultWithPresenceRuleId,
      title: "Required property has a fabricated empty default",
      messageFormat: "Property '{0}' has a NotEmpty()/NotNull() rule but is initialized with an empty string default that hides an unset value; use '= null!' or 'required' instead",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Initializing a required property with = string.Empty / = \"\" lets an omitted value slip past the presence rule as a valid-looking empty. Use = null! (or required) so an unset value is caught."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(NullableWithPresenceRule, EmptyDefaultWithPresenceRule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeRuleFor, SyntaxKind.InvocationExpression);
  }

  private static void AnalyzeRuleFor(SyntaxNodeAnalysisContext context)
  {
    var invocation = (InvocationExpressionSyntax)context.Node;

    // Must be a RuleFor(...) call with a single argument.
    if (!IsNamed(invocation.Expression, "RuleFor"))
      return;

    if (invocation.ArgumentList.Arguments.Count != 1)
      return;

    // Argument must be a simple lambda whose body is `param.Prop` (member access on the parameter).
    if (invocation.ArgumentList.Arguments[0].Expression is not SimpleLambdaExpressionSyntax lambda)
      return;

    if (lambda.Body is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax parameterRef } memberAccess)
      return;

    if (parameterRef.Identifier.ValueText != lambda.Parameter.Identifier.ValueText)
      return;

    // Only act when this RuleFor chain applies a presence rule.
    if (!ChainHasPresenceRule(invocation))
      return;

    // Resolve the validated type T from the enclosing AbstractValidator<T>.
    if (invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration)
      return;

    if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol validatorType)
      return;

    INamedTypeSymbol? validatedType = GetValidatedType(validatorType);
    if (validatedType is null)
      return;

    // Resolve the property on T and its declaration.
    string propertyName = memberAccess.Name.Identifier.ValueText;
    IPropertySymbol? property = validatedType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
    if (property is null || !property.Type.IsReferenceType)
      return;

    if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not PropertyDeclarationSyntax propertyDeclaration)
      return;

    Location location = propertyDeclaration.Identifier.GetLocation();

    // Contradiction A: declared nullable (string?) + presence rule.
    if (propertyDeclaration.Type is NullableTypeSyntax)
    {
      context.ReportDiagnostic(Diagnostic.Create(NullableWithPresenceRule, location, propertyName));
      return;
    }

    // Contradiction B: non-nullable + `= string.Empty` / `= ""` + presence rule.
    if (IsEmptyStringInitializer(propertyDeclaration.Initializer?.Value))
    {
      context.ReportDiagnostic(Diagnostic.Create(EmptyDefaultWithPresenceRule, location, propertyName));
    }
  }

  private static bool IsNamed(ExpressionSyntax expression, string name) =>
    expression switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.ValueText == name,
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == name,
      _ => false
    };

  // Walks up the fluent chain from RuleFor(...) collecting the applied validator method names.
  private static bool ChainHasPresenceRule(InvocationExpressionSyntax ruleForInvocation)
  {
    SyntaxNode node = ruleForInvocation;
    while (node.Parent is MemberAccessExpressionSyntax memberAccess
      && memberAccess.Parent is InvocationExpressionSyntax outerInvocation)
    {
      string methodName = memberAccess.Name.Identifier.ValueText;
      if (methodName is "NotEmpty" or "NotNull")
        return true;

      node = outerInvocation;
    }

    return false;
  }

  private static INamedTypeSymbol? GetValidatedType(INamedTypeSymbol validatorType)
  {
    for (INamedTypeSymbol? baseType = validatorType.BaseType; baseType is not null; baseType = baseType.BaseType)
    {
      if (baseType.Name == "AbstractValidator" && baseType.TypeArguments.Length == 1)
        return baseType.TypeArguments[0] as INamedTypeSymbol;
    }

    return null;
  }

  private static bool IsEmptyStringInitializer(ExpressionSyntax? value) =>
    value switch
    {
      // = "" (empty string literal)
      LiteralExpressionSyntax literal =>
        literal.IsKind(SyntaxKind.StringLiteralExpression) && literal.Token.ValueText.Length == 0,
      // = string.Empty / = String.Empty
      MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Empty", Expression: var receiver } =>
        receiver is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword }
          || receiver is IdentifierNameSyntax { Identifier.ValueText: "String" or "string" },
      _ => false
    };
}
