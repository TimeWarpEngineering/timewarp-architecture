#region Purpose
// TWA0021: MockAuthenticationStateProvider / MockAccessTokenProvider DI registrations must live
// only in MockAuthenticationRegistration so the fail-closed environment+config gate cannot be
// bypassed (task 145-009).
#endregion

#region Design
// Prefer analyzers over convention-by-memory: a casual AddScoped of MockAuthenticationStateProvider
// at a Program call site would skip MockAuthenticationDefaults and activate mock auth without the
// Development/Testing gate. Restrict references to the mock types, in any DI-registration shape, to
// the containing type named MockAuthenticationRegistration:
//   (a) generic type-argument form — AddScoped<TMock>() / AddScoped<IFace, TMock>()
//   (b) non-generic typeof(...) form — AddScoped(typeof(IFace), typeof(TMock))
//   (c) factory-delegate form — AddScoped<IFace>(_ => new TMock()) (object-creation anywhere in the
//       lambda body, including target-typed `new()`)
// Round-2 review (145-009) empirically proved (a)-only checking evaded via (b) and (c); both were
// zero-diagnostic bypasses that still activated mock auth outside the gated registration site.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MockAuthenticationRegistrationAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0021";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Mock auth providers must register only via MockAuthenticationRegistration",
      messageFormat: "Type '{0}' must not be used in a DI registration outside MockAuthenticationRegistration; use MockAuthenticationRegistration.TryAddSpaMockAuthentication so the Development/Testing + Authentication:UseMock fail-closed gate stays enforced",
      category: "Security",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "SPA mock authentication is fail-closed (Development/Testing and Authentication:UseMock). Direct DI registration of MockAuthenticationStateProvider or MockAccessTokenProvider bypasses that gate."
    );

  private static readonly ImmutableHashSet<string> MockTypeNames =
    ImmutableHashSet.Create
    (
      StringComparer.Ordinal,
      "MockAuthenticationStateProvider",
      "MockAccessTokenProvider"
    );

  private static readonly ImmutableHashSet<string> DiMethodNames =
    ImmutableHashSet.Create
    (
      StringComparer.Ordinal,
      "AddScoped",
      "AddSingleton",
      "AddTransient",
      "TryAddScoped",
      "TryAddSingleton",
      "TryAddTransient",
      "TryAddEnumerable",
      "Replace"
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
  }

  private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
  {
    // Product composition only — in-proc test harnesses (WebTestServerApplication) legitimately
    // DI-replace MockAccessTokenProvider (task 145-009 requirement 3).
    string path = context.Node.SyntaxTree.FilePath ?? string.Empty;
    if (path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
      || path.Contains("/tests/", StringComparison.OrdinalIgnoreCase)
      || path.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    var invocation = (InvocationExpressionSyntax)context.Node;
    string? methodName = GetInvokedMethodName(invocation);
    if (methodName is null || !DiMethodNames.Contains(methodName))
      return;

    INamedTypeSymbol? containingType = context.ContainingSymbol?.ContainingType;
    if (containingType is not null
      && string.Equals(containingType.Name, "MockAuthenticationRegistration", StringComparison.Ordinal))
    {
      return;
    }

    // (a) generic type-argument form: AddScoped<TMock>() / AddScoped<IFace, TMock>()
    foreach (TypeSyntax typeSyntax in CollectTypeArguments(invocation))
    {
      ITypeSymbol? type = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken).Type;
      ReportIfMockType(context, typeSyntax.GetLocation(), type, (typeSyntax as IdentifierNameSyntax)?.Identifier.ValueText);
    }

    foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
    {
      // (b) non-generic typeof(...) form: AddScoped(typeof(IFace), typeof(TMock))
      if (argument.Expression is TypeOfExpressionSyntax typeOfExpression)
      {
        ITypeSymbol? type = context.SemanticModel.GetTypeInfo(typeOfExpression.Type, context.CancellationToken).Type;
        ReportIfMockType(context, typeOfExpression.Type.GetLocation(), type, (typeOfExpression.Type as IdentifierNameSyntax)?.Identifier.ValueText);
      }

      // (c) factory-delegate form: AddScoped<IFace>(_ => new TMock()) — inspect every object
      // creation anywhere in the lambda body, not just its immediate return expression, so a
      // helper-local or multi-statement body cannot hide the mock type from this check.
      if (argument.Expression is AnonymousFunctionExpressionSyntax lambda)
      {
        foreach (BaseObjectCreationExpressionSyntax creation in lambda.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
        {
          ITypeSymbol? type = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken).Type;
          string? fallbackName = (creation as ObjectCreationExpressionSyntax)?.Type is IdentifierNameSyntax identifier
            ? identifier.Identifier.ValueText
            : null;
          ReportIfMockType(context, creation.GetLocation(), type, fallbackName);
        }
      }
    }
  }

  private static void ReportIfMockType(SyntaxNodeAnalysisContext context, Location location, ITypeSymbol? type, string? fallbackName)
  {
    string? name = type?.Name ?? fallbackName;
    if (name is null || !MockTypeNames.Contains(name))
      return;

    context.ReportDiagnostic(Diagnostic.Create(Rule, location, name));
  }

  private static string? GetInvokedMethodName(InvocationExpressionSyntax invocation) =>
    invocation.Expression switch
    {
      MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
      IdentifierNameSyntax id => id.Identifier.ValueText,
      GenericNameSyntax generic => generic.Identifier.ValueText,
      MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
      _ => null
    };

  private static IEnumerable<TypeSyntax> CollectTypeArguments(InvocationExpressionSyntax invocation)
  {
    if (invocation.Expression is MemberAccessExpressionSyntax
        {
          Name: GenericNameSyntax memberGeneric
        })
    {
      foreach (TypeSyntax arg in memberGeneric.TypeArgumentList.Arguments)
        yield return arg;
    }

    if (invocation.Expression is GenericNameSyntax generic)
    {
      foreach (TypeSyntax arg in generic.TypeArgumentList.Arguments)
        yield return arg;
    }
  }
}
