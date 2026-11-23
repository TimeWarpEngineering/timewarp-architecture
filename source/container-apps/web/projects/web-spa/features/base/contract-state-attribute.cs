#region Purpose
// [ContractState(typeof(...), ...)]: names the contracts a state sends, so the ContractState generator emits each
// contract's action set and TimeWarp.Mediator and TimeWarp.State wire it up (task 483).
#endregion

#region Design
// Derives from TimeWarp.Mediator.GeneratedRequestPatternAttribute with const templates. Roslyn generators do not see
// each other's output, so Mediator (Send arm, handler) and TimeWarp.State (dispatch method, catalog entry, manifest
// row) read these names instead of the generated Action. The templates must match what ContractStateSourceGenerator
// emits; Mediator's TWM013 and State's TWS0015 report it when they drift. The constants are fields so they survive in
// a reference assembly, which is how Mediator reads a derived attribute.
// It lives beside IBaseAction and DefaultApiHandler because the generator looks those up in this namespace.
// DraftRequests: contracts whose request is filled from local draft state, so the generator declares the required
// Build<Contract>Request hook instead of the optional OnBuilding<Contract>Request.
// ApiService: the IApiService the handlers inject; IWebServerApiService when not set.
// Hook rules and the generated shapes are in ContractStateSourceGenerator's Design region
// (source/analyzers/timewarp-architecture-analyzers/generators/).
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Generates a contract-bound action set on this state for each contract.</summary>
/// <param name="contracts">The [ApiEndpoint] contract classes this state sends.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ContractStateAttribute(params Type[] contracts) : GeneratedRequestPatternAttribute(contracts)
{
  /// <summary>The generated Action, per contract.</summary>
  public const string RequestTypeName = "{Host}.{Contract}ActionSet.Action";

  /// <summary>The generated Handler, per contract.</summary>
  public const string HandlerTypeName = "{Host}.{Contract}ActionSet.Handler";

  /// <summary>The Action is a void TimeWarp.Mediator action.</summary>
  public const string RequestInterfaceName = "TimeWarp.Mediator.IAction";

  /// <summary>Contracts whose request is built from local draft state (required Build&lt;Contract&gt;Request hook).</summary>
  public Type[] DraftRequests { get; set; } = [];

  /// <summary>The API service the generated handlers inject. Default: IWebServerApiService.</summary>
  public Type? ApiService { get; set; }
}
