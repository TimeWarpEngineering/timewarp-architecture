#region Purpose
// Enforces that Aspire AddProject resource names are ServiceNames constant values (TWA0007).
#endregion

#region Design
// Service discovery resolves each app's BaseAddress from services__{name}__ env vars that Aspire
// keys by resource name; a name that is not a ServiceNames value yields a null BaseAddress, and
// only under server-side rendering — a delayed runtime failure this makes a build error instead.
// The first argument is checked via semantic constant evaluation, so literals and const
// references (including aliases of ServiceNames itself) are all covered; non-constant names are
// skipped (nothing to verify at compile time). ServiceNames is resolved by its full metadata name
// — foundation-contracts ships as a package to generated apps, so the namespace is stable.
// Compilations that cannot see ServiceNames are silent (only the AppHost calls AddProject).
// AddYarp/AddPostgres/etc. are deliberately out of scope: only AddProject resources back the
// service-discovery names the apps resolve.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AspireResourceNameAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0007";

  private const string ServiceNamesMetadataName = "TimeWarp.Foundation.Configuration.ServiceNames";

  private static readonly DiagnosticDescriptor Rule =
    new
    (
      DiagnosticId,
      title: "Aspire resource name is not a ServiceNames constant value",
      messageFormat: "Aspire resource name '{0}' is not a ServiceNames constant value ({1}); service discovery resolves BaseAddress by these names and a mismatch yields null at runtime",
      category: "Design",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "AppHost AddProject resource names must equal the ServiceNames constants the applications use to resolve service URIs."
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(static startContext =>
    {
      INamedTypeSymbol? serviceNames = startContext.Compilation.GetTypeByMetadataName(ServiceNamesMetadataName);
      if (serviceNames is null) return;

      var allowedValues = new HashSet<string>(
        serviceNames.GetMembers().OfType<IFieldSymbol>()
          .Where(static f => f is { IsConst: true, HasConstantValue: true })
          .Select(static f => f.ConstantValue)
          .OfType<string>(),
        System.StringComparer.Ordinal);
      if (allowedValues.Count == 0) return;

      string allowedDisplay = string.Join(", ", allowedValues.OrderBy(static v => v, System.StringComparer.Ordinal));

      startContext.RegisterSyntaxNodeAction(
        nodeContext => AnalyzeInvocation(nodeContext, allowedValues, allowedDisplay),
        SyntaxKind.InvocationExpression);
    });
  }

  private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, HashSet<string> allowedValues, string allowedDisplay)
  {
    var invocation = (InvocationExpressionSyntax)context.Node;

    if (MethodName(invocation.Expression) != "AddProject") return;
    if (invocation.ArgumentList.Arguments.Count == 0) return;

    ExpressionSyntax nameArgument = invocation.ArgumentList.Arguments[0].Expression;
    Optional<object?> constantValue = context.SemanticModel.GetConstantValue(nameArgument, context.CancellationToken);
    if (constantValue is not { HasValue: true, Value: string resourceName }) return;

    if (!allowedValues.Contains(resourceName))
    {
      context.ReportDiagnostic(Diagnostic.Create(Rule, nameArgument.GetLocation(), resourceName, allowedDisplay));
    }
  }

  private static string? MethodName(ExpressionSyntax expression) =>
    expression switch
    {
      MemberAccessExpressionSyntax memberAccess => Identifier(memberAccess.Name),
      SimpleNameSyntax simpleName => Identifier(simpleName),
      _ => null
    };

  private static string Identifier(SimpleNameSyntax name) => name.Identifier.ValueText;
}
