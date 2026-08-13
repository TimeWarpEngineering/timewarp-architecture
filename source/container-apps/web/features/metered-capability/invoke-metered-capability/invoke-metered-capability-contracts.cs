#region Purpose
// Endpoint-centric contract for the metered pay-for-capability demo: expensive work that costs credit or x402.
#endregion

#region Design
// Host choice (104-011): **web-server** — agent bearer (AgentTokenDefaults) already exists here;
// api-server agent bearer is 104-030 and would block this demo if we waited. Route is under
// api/demo/... so free/discovery routes never share this path or payment middleware.
// [EndpointAuthorize] PermissionIds.DemoInvoke under agent-token only (182-006) — evaluator expands
// scope demo:invoke via AgentScopePermissionSeed. Payment is NOT authorization: permission proves
// the agent may attempt the capability; MeteredCapabilityGate bills credit or returns 402/503.
// Distinct from voluntary tip (104-009): every success debits the ledger. No GetMockResponseFactory
// — payment headers and principal identity are ambient, not mockable from SPA factories.
#endregion

namespace TimeWarp.Architecture.Features.MeteredCapability;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.DemoInvoke,
  AuthenticationSchemes = AuthenticationSchemeNames.AgentToken
)]
public static partial class InvokeMeteredCapability
{
  [ApiRoute("api/demo/metered-capability", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public string Message { get; }
    public decimal BalanceAfter { get; }
    public string FundingSource { get; }

    public Response(string message, decimal balanceAfter, string fundingSource)
    {
      Message = Guard.Against.NullOrEmpty(message);
      if (balanceAfter < 0m)
      {
        throw new ArgumentOutOfRangeException(nameof(balanceAfter), balanceAfter, "Balance cannot be negative.");
      }

      BalanceAfter = balanceAfter;
      FundingSource = Guard.Against.NullOrEmpty(fundingSource);
    }
  }
}
