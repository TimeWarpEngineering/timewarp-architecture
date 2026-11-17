#region Purpose
// Reads a [Page] NavPlacement argument as Top or Bottom without binding the generated enum.
#endregion

#region Design
// 1 is Bottom. The generated enum is not in this compilation, so a NavPlacement member has no
// constant value. A const field is followed to its initializer. A cast is read from its operand.
// An unbound NavPlacement.Bottom, or a using-static Bottom, is read by that name. Any other
// shape stays Top. The walk is bounded by NavPlacementWalkLimit so a const cycle cannot recurse.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Globalization;

/// <summary>Resolves the NavPlacement argument of [Page] to a pinned-bottom flag.</summary>
internal static class PageNavPlacementReader
{
  private const int NavPlacementWalkLimit = 8;

  internal static bool IsNavPinBottom(SemanticModel semanticModel, ExpressionSyntax expression) =>
    ReadNavPlacementValue(semanticModel, expression, 0) == 1;

  private static long? ReadNavPlacementValue(SemanticModel semanticModel, ExpressionSyntax expression, int depth)
  {
    if (depth > NavPlacementWalkLimit)
    {
      return null;
    }

    ExpressionSyntax current = expression;
    while (current is ParenthesizedExpressionSyntax parenthesized)
    {
      current = parenthesized.Expression;
    }

    if (current is CheckedExpressionSyntax checkedExpression)
    {
      return ReadNavPlacementValue(semanticModel, checkedExpression.Expression, depth + 1);
    }

    if (TryReadIntegralConstant(semanticModel, current, out long known))
    {
      return known;
    }

    if (current is CastExpressionSyntax cast)
    {
      return ReadNavPlacementValue(semanticModel, cast.Expression, depth + 1);
    }

    if (FindConstFieldInitializer(semanticModel, current) is { } initializer)
    {
      return ReadNavPlacementValue(initializer.SemanticModel, initializer.Expression, depth + 1);
    }

    return UnboundNavPlacementValue(semanticModel, current);
  }

  private static bool TryReadIntegralConstant(SemanticModel semanticModel, ExpressionSyntax expression, out long value)
  {
    value = 0;
    Optional<object?> constant = semanticModel.GetConstantValue(expression);
    if (!constant.HasValue || constant.Value is not (byte or sbyte or short or ushort or int or uint or long or ulong or Enum))
    {
      return false;
    }

    try
    {
      value = Convert.ToInt64(constant.Value, CultureInfo.InvariantCulture);
      return true;
    }
    catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
    {
      return false;
    }
  }

  private static (ExpressionSyntax Expression, SemanticModel SemanticModel)? FindConstFieldInitializer
  (
    SemanticModel semanticModel,
    ExpressionSyntax expression
  )
  {
    SymbolInfo info = semanticModel.GetSymbolInfo(expression);
    ISymbol? symbol = info.Symbol;
    if (symbol is null && info.CandidateSymbols.Length == 1)
    {
      symbol = info.CandidateSymbols[0];
    }

    if (symbol is not IFieldSymbol { IsConst: true } field)
    {
      return null;
    }

    foreach (SyntaxReference reference in field.DeclaringSyntaxReferences)
    {
      if (reference.GetSyntax() is not VariableDeclaratorSyntax declarator)
      {
        continue;
      }

      if (declarator.Initializer?.Value is not ExpressionSyntax value)
      {
        continue;
      }

      SemanticModel initializerModel = value.SyntaxTree == semanticModel.SyntaxTree
        ? semanticModel
        : semanticModel.Compilation.GetSemanticModel(value.SyntaxTree);
      return (value, initializerModel);
    }

    return null;
  }

  private static long? UnboundNavPlacementValue(SemanticModel semanticModel, ExpressionSyntax expression)
  {
    SymbolInfo info = semanticModel.GetSymbolInfo(expression);
    if (info.Symbol is not null || info.CandidateSymbols.Length > 0)
    {
      return null;
    }

    if (expression is IdentifierNameSyntax identifier)
    {
      return PlacementValue(identifier.Identifier.Text);
    }

    if (expression is MemberAccessExpressionSyntax member && IsNavPlacementTypeName(member.Expression))
    {
      return PlacementValue(member.Name.Identifier.Text);
    }

    return null;
  }

  private static long? PlacementValue(string name) => name switch
  {
    "Bottom" => 1,
    "Top" => 0,
    _ => null
  };

  private static bool IsNavPlacementTypeName(ExpressionSyntax expression)
  {
    ExpressionSyntax current = expression;
    while (current is MemberAccessExpressionSyntax member)
    {
      if (member.Name.Identifier.Text == "NavPlacement")
      {
        return true;
      }

      current = member.Expression;
    }

    return current switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.Text == "NavPlacement",
      AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text == "NavPlacement",
      _ => false
    };
  }
}
