#region Purpose
// Emits one per-assembly ActionRoster of every TimeWarp.State *ActionSet.Action in the assembly, plus
// the [CommandPaletteOptOut(reason)] marker, so the Ctrl-K palette lists actions by derivation (task 301).
#endregion

#region Design
// Discovery mirrors TimeWarp.State's own ActionSet method generator: a class whose name ends in
// "ActionSet", nested in a class (the state), with a nested class named "Action". Here the Action
// must also implement TimeWarp.Mediator.IAction (the ActionSet marker), so only real store actions are listed. No attribute
// opts an action in: registering an ActionSet is enough to appear in the roster.
// Parameters come from the Action's first explicit constructor, the same rule the State.Method
// generator uses. RequiredParameterCount counts parameters without a default. An action with no
// required parameter gets an Execute delegate that calls the generated State.Method with only
// externalCancellationToken, so the method's own defaults apply. No reflection: AOT and trim safe.
// Actions with required parameters get Execute = null, because the palette has no argument UI.
// Name is the TimeWarp.State catalog default, "<StateWithoutSuffix>.<ActionSetWithoutSuffix>", so
// an optional [CatalogAction] entry for the same Action type lines up with the roster entry.
// Opt-out is explicit: [CommandPaletteOptOut("reason")] on the Action class. The reason is read as a
// string literal (anything else reads as ""). The roster carries it verbatim; consumers treat a blank
// reason as no opt-out, and a guard test fails on one. The attribute is emitted here (as [Page] and
// [StateAccess] are) so it needs no runtime package.
// Owner/display text is not derived here: consumers humanize Name. Output is sorted by Name (ordinal).
// The roster is emitted only when the assembly declares at least one action.
// Incremental caching: ActionModel is a value-equatable record of strings and ints.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;

/// <summary>Source generator that emits ActionRoster (every *ActionSet.Action) and [CommandPaletteOptOut].</summary>
[Generator]
public sealed class ActionRosterSourceGenerator : IIncrementalGenerator
{
  private const string OptOutAttributeName = "CommandPaletteOptOut";
  private const string ActionInterfaceMetadataName = "TimeWarp.Mediator.IAction";

  /// <summary>Registers incremental generator steps for the action roster.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider.Select(
      static (p, _) => Sanitize(p.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? ns) && !string.IsNullOrWhiteSpace(ns)
        ? ns!
        : "Generated"));

    context.RegisterSourceOutput(rootNamespace, static (spc, ns) =>
      spc.AddSource("CommandPaletteOptOutAttribute.g.cs", SourceText.From(BuildAttribute(ns), Encoding.UTF8)));

    IncrementalValuesProvider<ActionModel> actions = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) => node is ClassDeclarationSyntax c
        && c.Identifier.Text.EndsWith("ActionSet", StringComparison.Ordinal)
        && c.Parent is ClassDeclarationSyntax,
      transform: static (ctx, ct) => GetAction(ctx, ct))
      .Where(static a => a is not null)
      .Select(static (a, _) => a!);

    context.RegisterSourceOutput(actions.Collect().Combine(rootNamespace), static (spc, pair) =>
    {
      if (pair.Left.IsDefaultOrEmpty) return;
      spc.AddSource("ActionRoster.g.cs", SourceText.From(EmitRoster(pair.Left, pair.Right), Encoding.UTF8));
    });
  }

  private sealed record ActionModel(
    string Name,
    string StateType,
    string ActionType,
    string MethodName,
    int RequiredParameterCount,
    string? OptOutReason);

  private static ActionModel? GetAction(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
  {
    var actionSet = (ClassDeclarationSyntax)ctx.Node;
    ClassDeclarationSyntax? actionClass = actionSet.Members
      .OfType<ClassDeclarationSyntax>()
      .FirstOrDefault(static c => c.Identifier.Text == "Action");
    if (actionClass is null) return null;

    if (ctx.SemanticModel.GetDeclaredSymbol(actionClass, ct) is not INamedTypeSymbol actionSymbol) return null;
    INamedTypeSymbol? actionSetSymbol = actionSymbol.ContainingType;
    INamedTypeSymbol? stateSymbol = actionSetSymbol?.ContainingType;
    if (actionSetSymbol is null || stateSymbol is null) return null;
    if (stateSymbol.IsGenericType || actionSetSymbol.IsGenericType || actionSymbol.IsGenericType) return null;
    if (!IsVisibleInAssembly(actionSymbol)) return null;
    if (!actionSymbol.AllInterfaces.Any(static i => i.ToDisplayString() == ActionInterfaceMetadataName)) return null;

    ConstructorDeclarationSyntax? constructor = actionClass.Members
      .OfType<ConstructorDeclarationSyntax>()
      .FirstOrDefault();
    int required = constructor?.ParameterList.Parameters.Count(static p => p.Default is null) ?? 0;

    string actionSetName = actionSetSymbol.Name;
    string methodName = actionSetName.Substring(0, actionSetName.Length - "ActionSet".Length);

    return new ActionModel(
      GetDefaultName(stateSymbol.Name, methodName),
      stateSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      actionSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      methodName,
      required,
      GetOptOutReason(actionClass));
  }

  /// <summary>TimeWarp.State's catalog default: &lt;StateWithoutSuffix&gt;.&lt;ActionSetWithoutSuffix&gt;.</summary>
  private static string GetDefaultName(string stateName, string methodName)
  {
    string state = stateName.EndsWith("State", StringComparison.Ordinal) && stateName.Length > "State".Length
      ? stateName.Substring(0, stateName.Length - "State".Length)
      : stateName;
    return $"{state}.{methodName}";
  }

  // null = no attribute; "" = attribute without a string-literal reason (consumers treat it as no opt-out).
  private static string? GetOptOutReason(ClassDeclarationSyntax actionClass)
  {
    AttributeSyntax? attribute = actionClass.AttributeLists
      .SelectMany(static l => l.Attributes)
      .FirstOrDefault(static a => StripAttribute(a.Name.ToString()) == OptOutAttributeName);
    if (attribute is null) return null;

    return attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal
      && literal.IsKind(SyntaxKind.StringLiteralExpression)
      ? literal.Token.ValueText
      : "";
  }

  private static bool IsVisibleInAssembly(INamedTypeSymbol symbol)
  {
    for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
    {
      if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
        return false;
    }

    return true;
  }

  private static string EmitRoster(ImmutableArray<ActionModel> actions, string rootNamespace)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("#nullable enable\n");
    sb.Append("namespace ").Append(rootNamespace).Append("\n{\n");
    sb.Append("  /// <summary>One TimeWarp.State action in this assembly (generated from its *ActionSet).</summary>\n");
    sb.Append("  /// <param name=\"Name\">&lt;StateWithoutSuffix&gt;.&lt;ActionSetWithoutSuffix&gt;, the TimeWarp.State catalog default.</param>\n");
    sb.Append("  /// <param name=\"RequiredParameterCount\">Constructor parameters without a default.</param>\n");
    sb.Append("  /// <param name=\"OptOutReason\">[CommandPaletteOptOut] reason verbatim; null when the action has no opt-out.</param>\n");
    sb.Append("  /// <param name=\"Execute\">Sends the action with no arguments; null when it has required parameters.</param>\n");
    sb.Append("  public sealed record ActionRosterEntry(\n");
    sb.Append("    string Name,\n");
    sb.Append("    global::System.Type StateType,\n");
    sb.Append("    global::System.Type ActionType,\n");
    sb.Append("    int RequiredParameterCount,\n");
    sb.Append("    string? OptOutReason,\n");
    sb.Append("    global::System.Func<global::TimeWarp.State.IStore, global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>? Execute);\n\n");
    sb.Append("  /// <summary>Every *ActionSet.Action in this assembly, sorted by Name.</summary>\n");
    sb.Append("  public static class ActionRoster\n  {\n");
    sb.Append("    public static global::System.Collections.Generic.IReadOnlyList<ActionRosterEntry> All { get; } =\n");
    sb.Append("    [\n");

    foreach (ActionModel action in actions
      .Distinct()
      .OrderBy(static a => a.Name, StringComparer.Ordinal)
      .ThenBy(static a => a.ActionType, StringComparer.Ordinal))
    {
      sb.Append("      new(").Append(Literal(action.Name)).Append(", ")
        .Append("typeof(").Append(action.StateType).Append("), ")
        .Append("typeof(").Append(action.ActionType).Append("), ")
        .Append(action.RequiredParameterCount).Append(", ")
        .Append(action.OptOutReason is null ? "null" : Literal(action.OptOutReason)).Append(", ");
      if (action.RequiredParameterCount == 0)
      {
        sb.Append("static (store, cancellationToken) => store.GetState<").Append(action.StateType).Append(">().")
          .Append(action.MethodName).Append("(externalCancellationToken: cancellationToken)");
      }
      else
      {
        sb.Append("null");
      }

      sb.Append("),\n");
    }

    sb.Append("    ];\n  }\n}\n");
    return sb.ToString();
  }

  private static string BuildAttribute(string ns)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(ns).Append("\n{\n");
    sb.Append("    /// <summary>Keeps this *ActionSet.Action out of the Ctrl-K palette. The reason is required: a blank one does not opt out.</summary>\n");
    sb.Append("    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]\n");
    sb.Append("    internal sealed class CommandPaletteOptOutAttribute : System.Attribute\n    {\n");
    sb.Append("        /// <summary>Why the palette must not offer this action.</summary>\n");
    sb.Append("        public string Reason { get; }\n");
    sb.Append("        public CommandPaletteOptOutAttribute(string reason) { Reason = reason; }\n");
    sb.Append("    }\n}\n");
    return sb.ToString();
  }

  private static string Literal(string value) =>
    Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

  private static string StripAttribute(string name)
  {
    int dot = name.LastIndexOf('.', StringComparison.Ordinal);
    if (dot >= 0) name = name.Substring(dot + 1);
    return name.EndsWith("Attribute", StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
  }

  private static string Sanitize(string ns)
  {
    var sb = new StringBuilder(ns.Length);
    foreach (char c in ns)
      sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' ? c : '_');
    return sb.ToString();
  }
}
