namespace TimeWarp.SourceCodeGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

/// <summary>
/// Generate the implementation of Delgate from a field marketd with a <see cref="DelegateAttribute"/> attribute
/// </summary>
[Generator]
public partial class DelegateSourceGenerator : ISourceGenerator
{

  private const string delegateAttributeSource = @"
namespace TimeWarp
{
  using System;

  /// <summary>
  /// Use this attribute to indicate that delegate source should be generated
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class DelegateAttribute : Attribute
    {
      public string PropertyName { get; set; }
    }
}
";

  public void Initialize(GeneratorInitializationContext aGeneratorInitializationContext)
  {

    // Register the attribute source
    aGeneratorInitializationContext
      .RegisterForPostInitialization
      (
        (context) =>
          {
            context.AddSource("DelegateAttribute.g.cs", delegateAttributeSource);
          }
      );

    // Register a syntax receiver that will be created for each generation pass
    aGeneratorInitializationContext.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    ;
  }

  public void Execute(GeneratorExecutionContext context)
  {
    // retreive the populated receiver 
    if (context.SyntaxContextReceiver is not SyntaxReceiver syntaxReceiver)
      return;

    // If the field type isn't an interface then we should show a Diagnostic Error

    List<string> log = new();

    foreach (FieldDeclarationSyntax fieldDeclarationSyntax in syntaxReceiver.CandidateFields)
    {
      log.Add(fieldDeclarationSyntax.ToFullString());
    }


    foreach (IFieldSymbol item in syntaxReceiver.Fields)
    {
      ITypeSymbol theInterfaceType = item.Type;
      string namespaceValue = item.ContainingNamespace.ToDisplayString();
      string classAccesibility = item.ContainingType.DeclaredAccessibility.ToString().ToLower();
      string fileName = item.ContainingType.Name;
      string className = item.ContainingType.Name;
      string typeArguments = string.Join(",", item.ContainingType.TypeArguments.Select(x => x.Name));
      if (!string.IsNullOrWhiteSpace(typeArguments)) typeArguments = $"<{typeArguments}>";

      List<string> interfaceSources = new();
      ImmutableArray<ISymbol> interfaceMembers = theInterfaceType.GetMembers();
      foreach (ISymbol interfaceMember in interfaceMembers)
      {
        string memberSource = GenerateSourceForMember(interfaceMember, theInterfaceType.ToDisplayString(), item.Name);
        if (!string.IsNullOrWhiteSpace(memberSource))
          interfaceSources.Add(memberSource);
      }
      string interfaceSource = string.Join("\n", interfaceSources);
      string source =
$@"#nullable enable
namespace {namespaceValue} {{

  {classAccesibility} partial class {className}{typeArguments}: {theInterfaceType.ToDisplayString()}
  {{
    {interfaceSource}
  }}
}}";

      source =
        SyntaxFactory
        .ParseCompilationUnit(source)
        .NormalizeWhitespace(indentation: "  ", eol: "\n")
        .GetText()
        .ToString();

      context.AddSource($"{fileName}.g.cs", source);
    }
  }

  private string GenerateSourceForMember(ISymbol interfaceMember, string interfaceName, string fieldName)
  {
    string result = "";
    switch (interfaceMember.Kind)
    {
      case SymbolKind.Alias:
        break;
      case SymbolKind.ArrayType:
        break;
      case SymbolKind.Assembly:
        break;
      case SymbolKind.DynamicType:
        break;
      case SymbolKind.ErrorType:
        break;
      case SymbolKind.Event:
        break;
      case SymbolKind.Field:
        break;
      case SymbolKind.Label:
        break;
      case SymbolKind.Local:
        break;
      case SymbolKind.Method:
        var methodSymbol = interfaceMember as IMethodSymbol;
        if (methodSymbol.MethodKind != MethodKind.PropertyGet && methodSymbol.MethodKind != MethodKind.PropertySet)
          result = GenerateMethod(methodSymbol, interfaceName, fieldName);
        break;
      case SymbolKind.NetModule:
        break;
      case SymbolKind.NamedType:
        break;
      case SymbolKind.Namespace:
        break;
      case SymbolKind.Parameter:
        break;
      case SymbolKind.PointerType:
        break;
      case SymbolKind.Property:
        result = GenerateProperty(interfaceMember, interfaceName, fieldName);
        break;
      case SymbolKind.RangeVariable:
        break;
      case SymbolKind.TypeParameter:
        break;
      case SymbolKind.Preprocessing:
        break;
      case SymbolKind.Discard:
        break;
      case SymbolKind.FunctionPointerType:
        break;
      default:
        throw new NotImplementedException();
    }

    return result;
  }

  private string GenerateProperty(ISymbol interfaceMember, string interfaceName, string fieldName)
  {
    //VS generated prop
    // public int MyProperty { get => Echo.MyProperty; set => Echo.MyProperty = value; }
    // public int MyProperty { get => Echo.MyProperty; set => Echo.MyProperty = value; }
    var propertySymbol = interfaceMember as IPropertySymbol;

    string accessibility = propertySymbol.DeclaredAccessibility.ToString().ToLower();
    string propertyType = propertySymbol.Type.ToDisplayString();
    string propertyName = propertySymbol.Name;
    string getter = propertySymbol.GetMethod != null ? $" get => {fieldName}.{propertyName};" : "";
    string setter = propertySymbol.SetMethod != null ? $"set => {fieldName}.{propertyName} = value; " : "";

    string template =
      $"{accessibility} {propertyType} {propertyName} {{{getter} {setter}}}";

    return template;
  }

  private string GenerateMethod(IMethodSymbol methodSymbol, string interfaceName, string fieldName)
  {
    ITypeSymbol returnType = methodSymbol.ReturnType;

    string parameterDeclarations = GenerateParameterDeclarations(methodSymbol);
    string parameterList = GenerateParameterInvocation(methodSymbol);
    string generic = methodSymbol.IsGenericMethod ? $"<{string.Join(",", methodSymbol.TypeParameters)}>" : string.Empty;

    string template =
      $"public {returnType.ToDisplayString()} {methodSymbol.Name}{generic}({parameterDeclarations}) => {fieldName}.{methodSymbol.Name}({parameterList});";

    //string template =
    //  $"{returnType.ToDisplayString()} {methodSymbol.OriginalDefinition} => {fieldName}.{methodSymbol.Name}({parameterList});";

    return template;
  }

  private string GenerateParameterInvocation(IMethodSymbol methodSymbol)
  {
    List<string> result = new();

    foreach (IParameterSymbol parameter in methodSymbol.Parameters)
    {
      result.Add($"{parameter.Name}");
    }

    return string.Join(", ", result);
  }
  private string GenerateParameterDeclarations(IMethodSymbol methodSymbol)
  {
    List<string> result = new();

    foreach (IParameterSymbol parameter in methodSymbol.Parameters)
    {
      string parameterDeclaration = $"{parameter.Type.ToDisplayString()} {parameter.Name}";
      if (parameter.HasExplicitDefaultValue)
      {
        parameterDeclaration += $" = {parameter.ExplicitDefaultValue ?? "default"}";
      }
      result.Add(parameterDeclaration);
    }

    return string.Join(", ", result);
  }

  /// <summary>
  /// Created on demand before each generation pass
  /// </summary>
  class SyntaxReceiver : ISyntaxContextReceiver
  {
    public List<FieldDeclarationSyntax> CandidateFields { get; } = new();
    public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

    /// <summary>
    /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
    /// </summary>
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
      // any field with at least one attribute is a candidate for property generation
      if
      (
        context.Node is FieldDeclarationSyntax fieldDeclarationSyntax &&
        fieldDeclarationSyntax.AttributeLists.Count > 0
      )
      {
        CandidateFields.Add(fieldDeclarationSyntax);

        foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
        {
          // Get the symbol being declared by the field, and keep it if its annotated
          var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
          if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "TimeWarp.DelegateAttribute"))
          {
            Fields.Add(fieldSymbol);
          }
        }
      }
    }
  }

  class DelegateThroughBuilder
  {
    private readonly RequiredStuff RequiredStuff;
    private readonly StringBuilder StringBuilder = new();

    public DelegateThroughBuilder(RequiredStuff aRequiredStuff)
    {
      RequiredStuff = aRequiredStuff;
    }

    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/interface

    // Implement Methods
    // Implement Properties
    // Implement Indexers
    // Implement Events
  }
}

internal class RequiredStuff
{
}
