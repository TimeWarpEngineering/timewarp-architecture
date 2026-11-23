#region Purpose
// Emits Command.From(I*Details) and a real Response.Clone() for edit-form contracts.
#endregion

#region Design
// The generator lives in foundation-contracts-generators because only that analyzer reference
// can add members to web-contracts. Convention analyzers report TWA0047–TWA0049; this generator
// stays silent when the type is not partial, already has From or Clone (including an explicit
// ICloneable.Clone), or has no parameterless constructor. Those answers come from
// DetailsCopyClassification.
// Version and other non-interface members stay off I*Details. From ends by calling the classic
// partial method CopyBeyondDetails(Command command, I*Details source). No access modifier, so a
// missing implementation is erased. UpdateSiteSettings.Command implements it for Version.
// Clone is MemberwiseClone plus a fresh collection (and ICloneable reference) for every field
// the snapshot must not share. Get-only and init-only auto-properties are written through
// UnsafeAccessor on the backing field, so Guard in the constructor does not run again during a
// state rollback. TimeWarp.State calls ICloneable.Clone() when that interface is present and
// otherwise copies backing fields itself; once Clone exists, the generated method is the copy.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

/// <summary>Generates details-interface copies for partial Command and Response contracts.</summary>
[Generator]
public sealed class DetailsCopyGenerator : IIncrementalGenerator
{
  /// <summary>Registers the syntax provider that emits one hint per contract type.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValuesProvider<DetailsCopyOutput> outputs = context.SyntaxProvider
      .CreateSyntaxProvider(
        static (node, _) => node is ClassDeclarationSyntax { Identifier.ValueText: "Command" or "Response" },
        static (syntax, token) => Read(syntax, token))
      .Where(static output => output.HintName.Length > 0);

    context.RegisterSourceOutput(
      outputs,
      static (production, output) => production.AddSource(output.HintName, SourceText.From(output.Source, Encoding.UTF8)));
  }

  private static DetailsCopyOutput Read(GeneratorSyntaxContext syntax, CancellationToken cancellationToken)
  {
    if (syntax.Node is not ClassDeclarationSyntax declaration)
    {
      return DetailsCopyOutput.None;
    }

    if (syntax.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
    {
      return DetailsCopyOutput.None;
    }

    if (type.DeclaringSyntaxReferences.Length == 0)
    {
      return DetailsCopyOutput.None;
    }

    SyntaxReference first = type.DeclaringSyntaxReferences[0];
    if (!first.SyntaxTree.Equals(declaration.SyntaxTree) || first.Span != declaration.Span)
    {
      return DetailsCopyOutput.None;
    }

    DetailsPlan? plan = DetailsCopyReader.TryPlan(type, cancellationToken);
    if (plan is null)
    {
      return DetailsCopyOutput.None;
    }

    string hint = "DetailsCopy." + Sanitize(type.ToDisplayString()) + ".g.cs";
    return new DetailsCopyOutput(hint, DetailsCopyEmitter.Emit(plan));
  }

  private static string Sanitize(string display)
  {
    char[] chars = display.ToCharArray();
    for (int index = 0; index < chars.Length; index++)
    {
      char current = chars[index];
      if (current is not ('.' or '_') && !char.IsLetterOrDigit(current))
      {
        chars[index] = '_';
      }
    }

    return new string(chars);
  }
}
