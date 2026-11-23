#region Purpose
// TWA0047–TWA0049: a mutable I*Details response must not share itself through Clone, and the
// command or response must be partial so From and Clone can be generated.
#endregion

#region Design
// I*Details matches ContractAgreementAnalyzer.IsDetailsInterface (longer than "IDetails", leading I,
// trailing Details). Identity Clone is only an error on a Response whose details interface has a
// non-init setter. Immutable Clone() => this stays legal. TWA0048 names each non-partial type in
// the nesting chain. TWA0049 is the parameterless-constructor gap, and only after the chain is
// already partial. Generated Clone is not re-analyzed (GeneratedCodeAnalysisFlags.None); a real
// MemberwiseClone body does not return this, so it stays quiet.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Roslyn analyzer for TWA0047–TWA0049: generated details copies have a real clone and a partial home.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DetailsCopyAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Diagnostic identifier TWA0047 (identity clone of a mutable details response).</summary>
  public const string IdentityCloneId = "TWA0047";

  /// <summary>Diagnostic identifier TWA0048 (type is not partial).</summary>
  public const string PartialId = "TWA0048";

  /// <summary>Diagnostic identifier TWA0049 (command has no parameterless constructor).</summary>
  public const string ConstructorId = "TWA0049";

  private const string Category = "Design";

  private static readonly DiagnosticDescriptor IdentityCloneRule = new(
    IdentityCloneId,
    title: "Mutable details response must not clone as itself",
    messageFormat: "'{0}' implements {1} with setters and Clone returns this, so a TimeWarp.State snapshot shares the instance; delete the identity Clone and mark the class partial so a deep Clone is generated",
    Category,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "TimeWarp.State calls ICloneable.Clone when the response implements it. Clone() => this shares the mutable instance with the snapshot.");

  private static readonly DiagnosticDescriptor PartialRule = new(
    PartialId,
    title: "Details command or response must be partial",
    messageFormat: "'{0}' must be partial so {1} can be generated for '{2}'",
    Category,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "From and Clone are generated into a partial class. A non-partial command, response, or containing type blocks that copy.");

  private static readonly DiagnosticDescriptor ConstructorRule = new(
    ConstructorId,
    title: "Details command needs a parameterless constructor",
    messageFormat: "'{0}' has no parameterless constructor, so From({1}) is not generated",
    Category,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "From constructs the command with new() before copying interface members. A command that only has parameterized constructors is left unchanged.");

  /// <summary>Diagnostics this analyzer reports.</summary>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    ImmutableArray.Create(IdentityCloneRule, PartialRule, ConstructorRule);

  /// <summary>Registers the named-type action that reports TWA0047–TWA0049.</summary>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
  }

  private static void AnalyzeNamedType(SymbolAnalysisContext context)
  {
    if (context.Symbol is not INamedTypeSymbol type)
    {
      return;
    }

    if (type.TypeKind != TypeKind.Class || type.IsStatic || type.IsAbstract || type.IsGenericType)
    {
      return;
    }

    if (type.Name is not ("Command" or "Response"))
    {
      return;
    }

    List<INamedTypeSymbol> details = DetailsInterfaces(type);
    if (details.Count == 0)
    {
      return;
    }

    if (type.Name == "Command")
    {
      AnalyzeCommand(context, type, details);
    }

    if (type.Name == "Response")
    {
      AnalyzeResponse(context, type, details);
    }
  }

  private static void AnalyzeCommand(SymbolAnalysisContext context, INamedTypeSymbol type, List<INamedTypeSymbol> details)
  {
    List<INamedTypeSymbol> missing = [];
    foreach (INamedTypeSymbol iface in details)
    {
      if (!HasFrom(type, iface))
      {
        missing.Add(iface);
      }
    }

    if (missing.Count == 0)
    {
      return;
    }

    if (!IsPartialChain(type, context.CancellationToken))
    {
      ReportNonPartial(context, type, "From");
      return;
    }

    if (!HasParameterlessConstructor(type))
    {
      string names = string.Join(", ", missing.Select(static iface => iface.Name));
      Location location = type.Locations.FirstOrDefault() ?? Location.None;
      context.ReportDiagnostic(Diagnostic.Create(ConstructorRule, location, type.Name, names));
    }
  }

  private static void AnalyzeResponse(SymbolAnalysisContext context, INamedTypeSymbol type, List<INamedTypeSymbol> details)
  {
    INamedTypeSymbol? mutable = null;
    foreach (INamedTypeSymbol iface in details)
    {
      if (IsMutable(iface))
      {
        mutable = iface;
        break;
      }
    }

    if (mutable is null)
    {
      return;
    }

    bool identity = CloneReturnsThis(type, context.CancellationToken);
    if (identity)
    {
      foreach (Location location in IdentityCloneLocations(type, context.CancellationToken))
      {
        context.ReportDiagnostic(Diagnostic.Create(IdentityCloneRule, location, type.Name, mutable.Name));
      }
    }

    if (!IsPartialChain(type, context.CancellationToken) && (!HasClone(type) || identity))
    {
      ReportNonPartial(context, type, "Clone");
    }
  }

  private static void ReportNonPartial(SymbolAnalysisContext context, INamedTypeSymbol type, string generatedMember)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
      foreach (SyntaxReference reference in current.DeclaringSyntaxReferences)
      {
        if (reference.GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax declaration)
        {
          continue;
        }

        if (declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)))
        {
          continue;
        }

        context.ReportDiagnostic(Diagnostic.Create(
          PartialRule,
          declaration.Identifier.GetLocation(),
          current.Name,
          generatedMember,
          type.Name));
      }
    }
  }

  private static bool CloneReturnsThis(INamedTypeSymbol type, CancellationToken cancellationToken)
  {
    foreach (ISymbol member in type.GetMembers("Clone"))
    {
      if (member is not IMethodSymbol { IsStatic: false } method || method.Parameters.Length != 0)
      {
        continue;
      }

      foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
      {
        if (reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax syntax && ReturnsThis(syntax))
        {
          return true;
        }
      }
    }

    return false;
  }

  private static IEnumerable<Location> IdentityCloneLocations(INamedTypeSymbol type, CancellationToken cancellationToken)
  {
    foreach (ISymbol member in type.GetMembers("Clone"))
    {
      if (member is not IMethodSymbol { IsStatic: false } method || method.Parameters.Length != 0)
      {
        continue;
      }

      foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
      {
        if (reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax syntax && ReturnsThis(syntax))
        {
          yield return syntax.Identifier.GetLocation();
        }
      }
    }
  }

  private static bool ReturnsThis(MethodDeclarationSyntax method)
  {
    if (method.ExpressionBody is not null)
    {
      return IsThis(method.ExpressionBody.Expression);
    }

    if (method.Body is null)
    {
      return false;
    }

    bool sawReturn = false;
    foreach (ReturnStatementSyntax statement in method.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
    {
      if (!IsMethodReturn(statement, method))
      {
        continue;
      }

      sawReturn = true;
      if (!IsThis(statement.Expression))
      {
        return false;
      }
    }

    return sawReturn;
  }

  private static bool IsMethodReturn(ReturnStatementSyntax statement, MethodDeclarationSyntax method)
  {
    for (SyntaxNode? current = statement.Parent; current is not null && current != method; current = current.Parent)
    {
      if (current is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsThis(ExpressionSyntax? expression)
  {
    return expression switch
    {
      ThisExpressionSyntax => true,
      CastExpressionSyntax cast => IsThis(cast.Expression),
      ParenthesizedExpressionSyntax parenthesized => IsThis(parenthesized.Expression),
      BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } binary => IsThis(binary.Left),
      _ => false
    };
  }

  private static bool IsPartialChain(INamedTypeSymbol type, CancellationToken cancellationToken)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
      if (current.DeclaringSyntaxReferences.Length == 0)
      {
        return false;
      }

      foreach (SyntaxReference reference in current.DeclaringSyntaxReferences)
      {
        if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration
          || !declaration.Modifiers.Any(static token => token.IsKind(SyntaxKind.PartialKeyword)))
        {
          return false;
        }
      }
    }

    return true;
  }

  private static bool HasFrom(INamedTypeSymbol type, INamedTypeSymbol details)
  {
    foreach (ISymbol member in type.GetMembers("From"))
    {
      if (member is IMethodSymbol { IsStatic: true } method
        && method.Parameters.Length == 1
        && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, details))
      {
        return true;
      }
    }

    return false;
  }

  private static bool HasClone(INamedTypeSymbol type)
  {
    foreach (ISymbol member in type.GetMembers("Clone"))
    {
      if (member is IMethodSymbol { IsStatic: false } method && method.Parameters.Length == 0)
      {
        return true;
      }
    }

    return false;
  }

  private static bool HasParameterlessConstructor(INamedTypeSymbol type)
  {
    foreach (IMethodSymbol constructor in type.InstanceConstructors)
    {
      if (constructor.Parameters.Length == 0)
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsMutable(INamedTypeSymbol details)
  {
    foreach (IPropertySymbol property in InterfaceProperties(details))
    {
      if (property.SetMethod is { IsInitOnly: false })
      {
        return true;
      }
    }

    return false;
  }

  private static List<INamedTypeSymbol> DetailsInterfaces(INamedTypeSymbol type)
  {
    List<INamedTypeSymbol> interfaces = [];
    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.TypeKind == TypeKind.Interface
        && iface.Name.Length > "IDetails".Length
        && iface.Name[0] == 'I'
        && iface.Name.EndsWith("Details", StringComparison.Ordinal))
      {
        interfaces.Add(iface);
      }
    }

    return interfaces;
  }

  private static List<IPropertySymbol> InterfaceProperties(INamedTypeSymbol details)
  {
    List<IPropertySymbol> properties = [];
    HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
    Collect(details, properties, seen);
    return properties;
  }

  private static void Collect(INamedTypeSymbol details, List<IPropertySymbol> properties, HashSet<INamedTypeSymbol> seen)
  {
    if (!seen.Add(details))
    {
      return;
    }

    foreach (ISymbol member in details.GetMembers())
    {
      if (member is IPropertySymbol { IsStatic: false, IsIndexer: false } property)
      {
        properties.Add(property);
      }
    }

    foreach (INamedTypeSymbol parent in details.Interfaces)
    {
      Collect(parent, properties, seen);
    }
  }
}
