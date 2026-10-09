#region Purpose
// Holds the payload a catalog action wants an agent caller to see after Execute returns.
#endregion

#region Design
// TimeWarp.State Execute is void. Submit, list, and open write the response here; CatalogAgentFunction
// and WebMcpDispatcher read it after the call and clear it before the next one. A human dispatch
// that nobody reads leaves the last payload until the next agent call clears it.
// Scoped to the circuit so two callers do not share a payload.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

public sealed class AgentCallOutcome
{
  public object? Result { get; private set; }

  public void Clear() => Result = null;

  public void Set(object result)
  {
    ArgumentNullException.ThrowIfNull(result);
    Result = result;
  }
}
