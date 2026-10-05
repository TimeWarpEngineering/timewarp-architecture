#region Purpose
// Emits the nested Offer record and OfferName constant onto every [Offerable] contract (task 281), or TWE012/TWE013 when the contract cannot be offered.
#endregion

#region Design
// An offer is the contract's Command minus server-filled fields, split into "bound by the server"
// (the Offer record's properties) and "typed by the user" ([Offerable] UserInput). Emitted onto the
// contract's static partial class in its own `{fqn}.Offer.g.cs`:
//   public const string OfferName = "<Slice>.<Operation>";
//   [global::<attributes namespace>.ActionOffer(OfferName, UserInput = ["nickname"])]
//   public sealed partial record Offer(global::System.Guid CredentialId);
// Bound properties, in order: the Command's [ApiRoute] parameters (parsed from the template with the
// same token rules as the route members, because those members are generated in this same pass and
// are invisible to the semantic model), then the Command's public settable instance properties
// (own and inherited, declaration order), minus UserInput entries. Auth-filled exclusion rule: a
// Command that implements IAuthApiRequest (declared or via [AuthApiRequest]) gets UserId from the
// caller's identity on the client and the server never trusts it, so UserId is never offered.
// IAuthApiRequest is the only member the contract pattern fills from identity today; a new
// identity-filled member joins this rule.
// OfferName naming scheme: "<Slice>.<Operation>" — Slice is the namespace segment after the last
// "Features" segment (the TWA0009 slice id; the last segment when there is none), Operation is the
// contract class name. Identity's RenameCredential offers "Identity.RenameCredential". The client
// action adopts it with [CatalogAction(Name = RenameCredential.OfferName)] (TWA0031 checks it).
// [ActionOffer] UserInput carries catalog PARAMETER names, so UserInput property names are
// camelCased exactly like the contract seam's System.Text.Json policy (CamelCase below mirrors
// ActionOfferAgreementAnalyzer.CamelCase). [ActionOffer] is resolved from the [Offerable]
// attribute's own namespace (same assembly), so the generator never spells the sourceName-rewritten
// Attributes namespace; [Offerable] is matched by simple name like the convention analyzers.
// The record is partial so the contract can add interfaces in its own declaration
// (`partial record Offer : ICredentialActionOffer;`).
// Fail-closed: TWE013 when there is no nested Command class, TWE012 for each UserInput entry that
// names no Command property; either way nothing is emitted for that contract. Descriptors live in
// the TWE SSOT (diagnostic-descriptors.cs, linked into this project).
// Incremental: a syntax predicate on partial classes with an Offerable-named attribute, then a
// transform to the equatable OfferTarget (strings, ImmutableArrays compared by sequence,
// DiagnosticInfo instead of Location), so trivia-only edits do not re-emit (239-001 lesson).
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TimeWarp.Architecture.Analyzers;

public sealed partial class ContractsGenerator
{
  private const string OfferableAttributeName = "OfferableAttribute";
  private const string AuthApiRequestInterfaceName = "IAuthApiRequest";
  private const string AuthFilledUserId = "UserId";
  private const string FeaturesSegment = "Features";

  private readonly record struct OfferProperty(string Type, string Name);

  private readonly record struct DiagnosticInfo(string Id, string FilePath, TextSpan Span, LinePositionSpan LineSpan, string Arg0, string Arg1)
  {
    public Diagnostic ToDiagnostic()
    {
      DiagnosticDescriptor descriptor = Id == DiagnosticDescriptors.OfferableUnknownUserInput.Id
        ? DiagnosticDescriptors.OfferableUnknownUserInput
        : DiagnosticDescriptors.OfferableMissingCommand;
      return Diagnostic.Create(descriptor, Location.Create(FilePath, Span, LineSpan), Arg0, Arg1);
    }
  }

  private readonly record struct OfferTarget(
    string Namespace,
    ImmutableArray<string> Containers,
    string ClassName,
    string HintBase,
    string OfferName,
    string ActionOfferAttribute,
    ImmutableArray<OfferProperty> Properties,
    ImmutableArray<string> UserInput,
    ImmutableArray<DiagnosticInfo> Diagnostics)
  {
    public bool Equals(OfferTarget other) =>
      Namespace == other.Namespace
      && ClassName == other.ClassName
      && HintBase == other.HintBase
      && OfferName == other.OfferName
      && ActionOfferAttribute == other.ActionOfferAttribute
      && Containers.SequenceEqual(other.Containers)
      && Properties.SequenceEqual(other.Properties)
      && UserInput.SequenceEqual(other.UserInput)
      && Diagnostics.SequenceEqual(other.Diagnostics);

    public override int GetHashCode()
    {
      HashCode hashCode = new();
      hashCode.Add(HintBase);
      hashCode.Add(OfferName);
      foreach (OfferProperty property in Properties)
        hashCode.Add(property);
      foreach (string input in UserInput)
        hashCode.Add(input);
      foreach (DiagnosticInfo diagnostic in Diagnostics)
        hashCode.Add(diagnostic);
      return hashCode.ToHashCode();
    }
  }

  private static void InitializeOffers(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValuesProvider<OfferTarget> offers = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsPartialClass(node) && HasOfferableAttributeSyntax((ClassDeclarationSyntax)node),
        transform: static (ctx, cancellationToken) => TransformOffer(ctx, cancellationToken))
      .Where(static t => t is not null)
      .Select(static (t, _) => t!.Value);

    context.RegisterSourceOutput(offers, static (spc, offer) =>
    {
      if (offer.Diagnostics.Length > 0)
      {
        foreach (DiagnosticInfo diagnostic in offer.Diagnostics)
          spc.ReportDiagnostic(diagnostic.ToDiagnostic());
        return;
      }

      spc.AddSource($"{offer.HintBase}.Offer.g.cs", SourceText.From(WrapOffer(offer), Encoding.UTF8));
    });
  }

  private static bool HasOfferableAttributeSyntax(ClassDeclarationSyntax declaration)
  {
    foreach (AttributeListSyntax list in declaration.AttributeLists)
    {
      foreach (AttributeSyntax attribute in list.Attributes)
      {
        string name = attribute.Name switch
        {
          QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
          AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
          SimpleNameSyntax simple => simple.Identifier.Text,
          _ => attribute.Name.ToString()
        };
        if (name is "Offerable" or OfferableAttributeName)
          return true;
      }
    }

    return false;
  }

  private static OfferTarget? TransformOffer(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol contract)
      return null;

    // A partial contract has one [Offerable] but several declarations; emit from the one that carries it.
    AttributeData? offerable = contract.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.Name == OfferableAttributeName);
    if (offerable?.ApplicationSyntaxReference is not { } reference
      || !context.Node.Span.Contains(reference.Span)
      || reference.SyntaxTree != context.Node.SyntaxTree)
    {
      return null;
    }

    if (ToTarget(contract, ImmutableArray<Part>.Empty) is not { } identity)
      return null;

    Location attributeLocation = reference.GetSyntax(cancellationToken).GetLocation();
    string offerName = OfferName(identity.Namespace, contract.Name);
    string actionOfferAttribute = "global::" + offerable.AttributeClass!.ContainingNamespace.ToDisplayString() + ".ActionOfferAttribute";
    OfferTarget Empty(ImmutableArray<DiagnosticInfo> diagnostics) =>
      new(identity.Namespace, identity.Containers, identity.ClassName, identity.HintBase, offerName, actionOfferAttribute, [], [], diagnostics);

    INamedTypeSymbol? command = contract.GetTypeMembers("Command").FirstOrDefault(static t => t.TypeKind == TypeKind.Class);
    if (command is null)
      return Empty([Info(DiagnosticDescriptors.OfferableMissingCommand, attributeLocation, contract.ToDisplayString(), "")]);

    List<OfferProperty> candidates = [.. RouteProperties(command)];
    foreach (IPropertySymbol property in CommandProperties(command))
    {
      if (candidates.Any(candidate => candidate.Name == property.Name))
        continue;
      candidates.Add(new OfferProperty(property.Type.ToDisplayString(PropertyTypeFormat), property.Name));
    }

    if (IsAuthFilled(command))
      candidates.RemoveAll(static candidate => candidate.Name == AuthFilledUserId);

    ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
    List<string> userInput = [];
    foreach (string input in UserInputNames(offerable))
    {
      int index = candidates.FindIndex(candidate => candidate.Name == input);
      if (index < 0)
      {
        diagnostics.Add(Info(DiagnosticDescriptors.OfferableUnknownUserInput, attributeLocation, contract.ToDisplayString(), input));
        continue;
      }

      candidates.RemoveAt(index);
      userInput.Add(CamelCase(input));
    }

    if (diagnostics.Count > 0)
      return Empty(diagnostics.ToImmutable());

    return new OfferTarget(
      identity.Namespace,
      identity.Containers,
      identity.ClassName,
      identity.HintBase,
      offerName,
      actionOfferAttribute,
      [.. candidates],
      [.. userInput],
      []);
  }

  private static readonly SymbolDisplayFormat PropertyTypeFormat =
    SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

  private static DiagnosticInfo Info(DiagnosticDescriptor descriptor, Location location, string arg0, string arg1)
  {
    FileLinePositionSpan lineSpan = location.GetLineSpan();
    return new DiagnosticInfo(descriptor.Id, lineSpan.Path, location.SourceSpan, lineSpan.Span, arg0, arg1);
  }

  /// <summary>Route parameters of the Command's first [ApiRoute], typed like the generated route members.</summary>
  private static IEnumerable<OfferProperty> RouteProperties(INamedTypeSymbol command)
  {
    AttributeData? route = command.GetAttributes().FirstOrDefault(static a =>
      a.AttributeClass is { Name: "ApiRouteAttribute" } attributeClass
      && attributeClass.ContainingNamespace?.ToDisplayString() == AttributeNamespace);
    if (route?.ConstructorArguments.FirstOrDefault().Value is not string template)
      yield break;

    foreach (string segment in template.Split('/'))
    {
      Match match = RouteParam().Match(segment);
      if (!match.Success)
        continue;

      string constraint = match.Groups[2].Success ? match.Groups[2].Value : "string";
      yield return new OfferProperty(MapConstraintToClrType(constraint), match.Groups[1].Value);
    }
  }

  /// <summary>Public settable instance properties of the Command and its base types (declaration order, most-derived first).</summary>
  private static IEnumerable<IPropertySymbol> CommandProperties(INamedTypeSymbol command)
  {
    HashSet<string> seen = new(StringComparer.Ordinal);
    for (INamedTypeSymbol? type = command; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
    {
      foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.IsStatic || property.IsIndexer || property.SetMethod is null) continue;
        if (property.DeclaredAccessibility != Accessibility.Public) continue;
        if (!seen.Add(property.Name)) continue;
        yield return property;
      }
    }
  }

  /// <summary>The Command's UserId is filled from the caller's identity: IAuthApiRequest declared, or [AuthApiRequest] (which generates it).</summary>
  private static bool IsAuthFilled(INamedTypeSymbol command) =>
    command.AllInterfaces.Any(static i => i.Name == AuthApiRequestInterfaceName)
    || command.GetAttributes().Any(static a =>
      a.AttributeClass is { Name: "AuthApiRequestAttribute" } attributeClass
      && attributeClass.ContainingNamespace?.ToDisplayString() == AttributeNamespace)
    || command.DeclaringSyntaxReferences
      .Select(static r => r.GetSyntax())
      .OfType<ClassDeclarationSyntax>()
      .Any(static d => d.BaseList?.Types.Any(static t => t.Type.ToString().EndsWith(AuthApiRequestInterfaceName, StringComparison.Ordinal)) == true);

  private static IEnumerable<string> UserInputNames(AttributeData offerable)
  {
    foreach (KeyValuePair<string, TypedConstant> argument in offerable.NamedArguments)
    {
      if (argument.Key == "UserInput" && argument.Value.Kind == TypedConstantKind.Array)
        return argument.Value.Values.Select(static value => value.Value).OfType<string>();
    }

    return [];
  }

  /// <summary>"&lt;Slice&gt;.&lt;Operation&gt;": the namespace segment after the last "Features" (else the last segment), then the contract name.</summary>
  private static string OfferName(string ns, string contractName)
  {
    string[] segments = ns.Split('.');
    int features = Array.LastIndexOf(segments, FeaturesSegment);
    string slice = features >= 0 && features + 1 < segments.Length ? segments[features + 1] : segments[segments.Length - 1];
    return slice + "." + contractName;
  }

  /// <summary>System.Text.Json's JsonNamingPolicy.CamelCase (the contract seam's policy).</summary>
  private static string CamelCase(string name)
  {
    if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
      return name;

    StringBuilder builder = new(name);
    for (int i = 0; i < builder.Length; i++)
    {
      if (i == 1 && !char.IsUpper(builder[i]))
        break;

      bool hasNext = i + 1 < builder.Length;
      if (i > 0 && hasNext && !char.IsUpper(builder[i + 1]))
      {
        if (builder[i + 1] == ' ')
          builder[i] = char.ToLowerInvariant(builder[i]);
        break;
      }

      builder[i] = char.ToLowerInvariant(builder[i]);
    }

    return builder.ToString();
  }

  private static string WrapOffer(OfferTarget offer)
  {
    StringBuilder sb = new();
    sb.Append("// <auto-generated/>\n#nullable enable\n");
    sb.Append("namespace ").Append(offer.Namespace).Append(";\n\n");

    int indent = 0;
    foreach (string container in offer.Containers)
    {
      sb.Append(Indent(indent)).Append("partial class ").Append(container).Append('\n');
      sb.Append(Indent(indent)).Append("{\n");
      indent++;
    }

    string body = Indent(indent + 1);
    sb.Append(Indent(indent)).Append("partial class ").Append(offer.ClassName).Append('\n');
    sb.Append(Indent(indent)).Append("{\n");
    sb.Append(body).Append("/// <summary>Catalog name the server offers this operation under; the client action sets <c>[CatalogAction(Name = OfferName)]</c>.</summary>\n");
    sb.Append(body).Append("public const string OfferName = \"").Append(offer.OfferName).Append("\";\n\n");
    sb.Append(body).Append("/// <summary>Arguments the server binds when it offers this operation (the Command minus user input and auth-filled fields).</summary>\n");
    sb.Append(body).Append('[').Append(offer.ActionOfferAttribute).Append("(OfferName");
    if (offer.UserInput.Length > 0)
      sb.Append(", UserInput = [").Append(string.Join(", ", offer.UserInput.Select(static input => "\"" + input + "\""))).Append(']');
    sb.Append(")]\n");
    sb.Append(body).Append("public sealed partial record Offer");
    if (offer.Properties.Length > 0)
      sb.Append('(').Append(string.Join(", ", offer.Properties.Select(static p => p.Type + " " + p.Name))).Append(')');
    sb.Append(";\n");
    sb.Append(Indent(indent)).Append("}\n");

    for (int i = offer.Containers.Length - 1; i >= 0; i--)
    {
      indent--;
      sb.Append(Indent(indent)).Append("}\n");
    }

    return sb.ToString();
  }
}
