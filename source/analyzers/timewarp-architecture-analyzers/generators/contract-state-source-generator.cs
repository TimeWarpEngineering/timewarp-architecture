#region Purpose
// Generates a contract-bound action set per contract named in [ContractState(typeof(...), ...)] on a TimeWarp.State
// state: the Action, its DefaultApiHandler, a typed dispatch overload, and the hooks a person writes (task 483).
#endregion

#region Design
// Who emits what. Roslyn generators do not see each other's output, so the Action this generator emits is invisible
// to TimeWarp.Mediator's and TimeWarp.State's generators. [ContractState] (web-spa, features/base) derives from
// TimeWarp.Mediator.GeneratedRequestPatternAttribute with const templates, and both libraries read that declaration
// instead of the Action:
// - TimeWarp.Mediator (15.0.0-beta.10, task 043) emits the Send arm for <State>.<Contract>ActionSet.Action and
//   resolves <State>.<Contract>ActionSet.Handler, with the same behavior pipeline as a source action.
// - TimeWarp.State (12.0.0-beta.18, task 160) emits a catalog entry and a manifest row (origin "declared") and an
//   internal dispatch method that takes the Action instance, because it cannot see the Action's constructor.
// - This generator emits the Action, the Handler, the hooks, and the public typed dispatch method
//   `<Contract>(TRequest request, CancellationToken? externalCancellationToken = null)`.
// The 140 report's open point (State exposes the dispatch rule as a reusable emitter, or this generator emits it) is
// settled as: this generator emits it, copying State's shape (linked token, Sender.Send). Reason: State's extractor
// (beta.19, DeclaredActionExtractor) reads a declaration only from the state's first partial declaration, and only
// when that declaration has an attribute list. RoleState's first declaration is role-state.create-role.cs, so State
// emits nothing for it; a typed overload that called State's method would not compile. Emitting the method here does
// not depend on file order. When State finds the declaration, its internal <Contract>(Action, ...) is an overload on
// a different parameter type, so the two never clash. Until State reads every declaration (task 483 Notes), the
// generated actions have no State catalog entry and no manifest row. Mediator reads each declaration that carries the
// attribute, so its Send arms do not have this limit.
// Catalog limit (State 12.0.0-beta.19): a declared action's catalog entry is Human-only, takes the Action as its one
// parameter, and reads Description and Permissions from attributes on the contract class (not from [Operation] on
// the request or [EndpointAuthorize].Policy). An agent tool with a typed parameter (Role.CreateRole) therefore stays
// a hand-written action set with [CatalogAction]; ContractCatalogAgreementAnalyzer checks that copy instead.
// Hook rules (464 §4f): a Query's success hook On<C>Loaded is required on every generated fetch, because the
// generator cannot tell from symbols whether the snapshot seeds a local draft, and a missing seed loses data on the
// next save. Hooks with an access modifier are required (CS8795 when the body is missing); partial void hooks are
// optional. The model and emitter carry the rules: ContractStateReader and ContractStateEmitter.
// Discovery: any class with an attribute named ContractState; the reader confirms the base chain semantically. The
// transform renders text, so the pipeline caches on the source string and the diagnostics. Generates no Razor.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>Source generator for [ContractState] contract-bound action sets.</summary>
[Generator]
public sealed class ContractStateSourceGenerator : IIncrementalGenerator
{
  /// <summary>Registers the [ContractState] pipeline.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValuesProvider<ContractStateModel> states = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) => ContractStateReader.IsCandidate(node),
      transform: static (ctx, ct) => ContractStateReader.Read(ctx, ct))
      .Where(static model => model is not null)
      .Select(static (model, _) => model!);

    context.RegisterSourceOutput(states, static (spc, model) =>
    {
      foreach (ContractStateDiagnostic diagnostic in model.Diagnostics)
      {
        spc.ReportDiagnostic(Diagnostic.Create(
          DiagnosticDescriptors.ContractStateInvalid,
          diagnostic.ToLocation(),
          model.StateName,
          diagnostic.Message));
      }

      if (model.Actions.Count == 0) return;
      spc.AddSource(model.HintName, SourceText.From(ContractStateEmitter.Emit(model), Encoding.UTF8));
    });
  }
}
