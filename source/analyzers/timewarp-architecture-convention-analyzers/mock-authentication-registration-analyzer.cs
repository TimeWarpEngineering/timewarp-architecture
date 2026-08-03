#region Purpose
// TWA0021: MockAuthenticationStateProvider / MockAccessTokenProvider DI registrations must live
// only in MockAuthenticationRegistration so the fail-closed environment+config gate cannot be
// bypassed (task 145-009).
#endregion

#region Design
// Prefer analyzers over convention-by-memory: a casual AddScoped of MockAuthenticationStateProvider
// at a Program call site would skip MockAuthenticationDefaults and activate mock auth without the
// Development/Testing gate. Restrict type references used as generic type arguments of
// AddScoped/AddSingleton/AddTransient/TryAdd* extension invocations to the containing type named
// MockAuthenticationRegistration.
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

    foreach (TypeSyntax typeSyntax in CollectTypeArguments(invocation))
    {
      ITypeSymbol? type = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken).Type;
      string? name = type?.Name ?? (typeSyntax as IdentifierNameSyntax)?.Identifier.ValueText;
      if (name is null || !MockTypeNames.Contains(name))
        continue;

      context.ReportDiagnostic(Diagnostic.Create(Rule, typeSyntax.GetLocation(), name));
    }
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
