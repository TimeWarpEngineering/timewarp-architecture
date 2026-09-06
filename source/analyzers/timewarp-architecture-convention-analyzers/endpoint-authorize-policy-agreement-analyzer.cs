#region Purpose
// Enforces TWA0024: a hosted [EndpointAuthorize] Policy name is registered by this server.
#endregion

#region Design
// Task 111: contract Policy strings cannot reference server-layer constants (wrong dependency
// direction), so without this check they agree only by comment. Fail-closed 401/403 catches
// drift at runtime; this analyzer catches it at the server build.
// Mechanism is an agreement check, not generation: web already has PermissionIds as the product
// policy SSOT (AddPermissionPolicies registers All); api-server still registers named policies via
// AddPolicy(AgentTokenDefaults.*). Generating one side from the other would duplicate PermissionIds
// or invert the contracts→server dependency. Checking values at the server compilation sees both
// sides (TWA0006 pairing: this assembly + *contracts with the same first name segment).
// Registered set = constant-evaluated AuthorizationOptions/AuthorizationBuilder.AddPolicy names
// plus, when PermissionPolicyRegistration.AddPermissionPolicies is called, PermissionIds public
// const strings except ClaimType (not a policy). CORS AddPolicy is ignored (different host type).
// ClientOnly / missing Policy / contracts-only compilations are silent. Reports on the Policy
// argument when syntax is available.
// RS1030: do not call Compilation.GetSemanticModel from a CompilationAction — collect AddPolicy
// via CompilationStart + SyntaxNodeAction, then compare at CompilationEnd.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointAuthorizePolicyAgreementAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "TWA0024";

  private const string Category = "Design";
  private const string PermissionIdsTypeName = "PermissionIds";
  private const string PermissionIdsClaimTypeField = "ClaimType";
  private const string PermissionPolicyRegistrationTypeName = "PermissionPolicyRegistration";

  private static readonly DiagnosticDescriptor UnregisteredPolicy =
    new
    (
      DiagnosticId,
      title: "[EndpointAuthorize] Policy is not registered by this server",
      messageFormat: "Contract '{0}' [EndpointAuthorize] Policy '{1}' is not registered by this server (registered: {2})",
      Category,
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Hosted [EndpointAuthorize] Policy values must equal a policy this server registers via AuthorizationOptions/AuthorizationBuilder.AddPolicy or PermissionIds (when AddPermissionPolicies is called). Contracts cannot reference server-layer constants, so the server compilation is the place the two sides can be checked.",
      customTags: WellKnownDiagnosticTags.CompilationEnd
    );

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(UnregisteredPolicy);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterCompilationStartAction(StartCompilation);
  }

  private static void StartCompilation(CompilationStartAnalysisContext context)
  {
    INamedTypeSymbol? baseFastEndpoint = context.Compilation.GetTypeByMetadataName("TimeWarp.Foundation.Features.BaseFastEndpoint`2");
    if (baseFastEndpoint is null) return;
    if (!HasEndpointSubclass(context.Compilation, baseFastEndpoint)) return;

    RegistrationCollector collector = new();

    context.RegisterSyntaxNodeAction(
      nodeContext => CollectRegistration(nodeContext, collector),
      SyntaxKind.InvocationExpression);

    context.RegisterCompilationEndAction(endContext => ReportUnregistered(endContext, collector));
  }

  private static bool HasEndpointSubclass(Compilation compilation, INamedTypeSymbol baseFastEndpoint)
  {
    foreach (INamedTypeSymbol type in HostedRouteDiscovery.GetAllTypes(compilation.Assembly.GlobalNamespace))
    {
      for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
      {
        if (SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, baseFastEndpoint))
        {
          return true;
        }
      }
    }

    return false;
  }

  private static void CollectRegistration(SyntaxNodeAnalysisContext context, RegistrationCollector collector)
  {
    var invocation = (InvocationExpressionSyntax)context.Node;
    string? methodName = MethodName(invocation.Expression);
    if (methodName is null) return;

    if (methodName == "AddPermissionPolicies")
    {
      IMethodSymbol? permissionMethod = ResolveMethod(context.SemanticModel, invocation, context.CancellationToken);
      if (permissionMethod?.ContainingType?.Name == PermissionPolicyRegistrationTypeName)
      {
        Interlocked.Exchange(ref collector.HarvestPermissionIds, 1);
      }

      return;
    }

    if (methodName != "AddPolicy") return;
    if (!invocation.ArgumentList.Arguments.Any()) return;

    IMethodSymbol? method = ResolveMethod(context.SemanticModel, invocation, context.CancellationToken);
    if (method is null || !IsAuthorizationAddPolicy(method)) return;

    ExpressionSyntax nameArgument = invocation.ArgumentList.Arguments[0].Expression;
    Optional<object?> constantValue = context.SemanticModel.GetConstantValue(nameArgument, context.CancellationToken);
    if (constantValue is { HasValue: true, Value: string policyName }
        && !string.IsNullOrWhiteSpace(policyName))
    {
      collector.Registered.TryAdd(policyName, 0);
    }
  }

  private static void ReportUnregistered(CompilationAnalysisContext context, RegistrationCollector collector)
  {
    HashSet<string> registered = [.. collector.Registered.Keys];
    if (Volatile.Read(ref collector.HarvestPermissionIds) != 0)
    {
      HarvestPermissionIdConstants(context.Compilation, registered);
    }

    string registeredDisplay = registered.Count == 0
      ? "(none)"
      : string.Join(", ", registered.OrderBy(static name => name, StringComparer.Ordinal));

    foreach (IAssemblySymbol assembly in HostedRouteDiscovery.GetPairedContractAssemblies(context.Compilation))
    {
      foreach (INamedTypeSymbol type in HostedRouteDiscovery.GetAllTypes(assembly.GlobalNamespace))
      {
        if (!HostedRouteDiscovery.TryGetHostedOperation(type, out _))
        {
          continue;
        }

        AttributeData? endpointAuthorize = type.GetAttributes()
          .FirstOrDefault(static attribute => attribute.AttributeClass?.Name == "EndpointAuthorizeAttribute");
        if (endpointAuthorize is null) continue;

        string? policyName = GetPolicyName(endpointAuthorize);
        if (policyName is null) continue;

        if (registered.Contains(policyName)) continue;

        Location location = PolicyLocation(endpointAuthorize, type, context.CancellationToken);
        context.ReportDiagnostic(Diagnostic.Create(
          UnregisteredPolicy,
          location,
          type.ToDisplayString(),
          policyName,
          registeredDisplay));
      }
    }
  }

  private static void HarvestPermissionIdConstants(Compilation compilation, HashSet<string> names)
  {
    HarvestPermissionIdConstants(compilation.Assembly.GlobalNamespace, names);
    foreach (IAssemblySymbol referenced in compilation.SourceModule.ReferencedAssemblySymbols)
    {
      HarvestPermissionIdConstants(referenced.GlobalNamespace, names);
    }
  }

  private static void HarvestPermissionIdConstants(INamespaceSymbol root, HashSet<string> names)
  {
    foreach (INamedTypeSymbol type in HostedRouteDiscovery.GetAllTypes(root))
    {
      if (type.Name != PermissionIdsTypeName) continue;

      foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
      {
        if (field is not { IsConst: true, HasConstantValue: true }) continue;
        if (field.Name == PermissionIdsClaimTypeField) continue;
        if (field.ConstantValue is string value && !string.IsNullOrWhiteSpace(value))
        {
          names.Add(value);
        }
      }
    }
  }

  private static bool IsAuthorizationAddPolicy(IMethodSymbol method)
  {
    INamedTypeSymbol? hostType = method.ContainingType;
    if (method.IsExtensionMethod && method.Parameters.Length > 0)
    {
      hostType = method.Parameters[0].Type as INamedTypeSymbol;
    }

    string? hostName = hostType?.Name;
    return hostName is "AuthorizationOptions" or "AuthorizationBuilder";
  }

  private static IMethodSymbol? ResolveMethod(
    SemanticModel model,
    InvocationExpressionSyntax invocation,
    CancellationToken cancellationToken)
  {
    SymbolInfo info = model.GetSymbolInfo(invocation, cancellationToken);
    if (info.Symbol is IMethodSymbol method)
    {
      return method;
    }

    return info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
  }

  private static string? GetPolicyName(AttributeData endpointAuthorize)
  {
    foreach (KeyValuePair<string, TypedConstant> argument in endpointAuthorize.NamedArguments)
    {
      if (argument.Key != "Policy") continue;
      string? value = argument.Value.Value?.ToString();
      return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    return null;
  }

  private static Location PolicyLocation(
    AttributeData endpointAuthorize,
    INamedTypeSymbol operationType,
    CancellationToken cancellationToken)
  {
    SyntaxNode? syntax = endpointAuthorize.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
    if (syntax is AttributeSyntax attributeSyntax)
    {
      AttributeArgumentSyntax? policyArgument = attributeSyntax.ArgumentList?.Arguments
        .FirstOrDefault(static argument => argument.NameEquals?.Name.Identifier.ValueText == "Policy");
      if (policyArgument is not null)
      {
        return policyArgument.Expression.GetLocation();
      }

      return attributeSyntax.GetLocation();
    }

    return operationType.Locations.FirstOrDefault() ?? Location.None;
  }

  private static string? MethodName(ExpressionSyntax expression) =>
    expression switch
    {
      MemberAccessExpressionSyntax memberAccess => Identifier(memberAccess.Name),
      SimpleNameSyntax simpleName => Identifier(simpleName),
      _ => null
    };

  private static string Identifier(SimpleNameSyntax name) => name.Identifier.ValueText;

  private sealed class RegistrationCollector
  {
    public ConcurrentDictionary<string, byte> Registered { get; } = new(StringComparer.Ordinal);
    public int HarvestPermissionIds;
  }
}
