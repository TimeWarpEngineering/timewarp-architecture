#region Purpose
// Reasoned opt-out from TWA0046 for a contract the SPA sends that cannot have a mock response factory.
#endregion

#region Design
// ContractSpaAgreementAnalyzer reports an SPA handler on IWebServerApiService (or a [ContractState] contract) whose
// contract has no GetMockResponseFactory(), because MockWebApiService then falls through to the real API with only a
// Debug log. A contract that cannot be answered as plain JSON (a passkey or Entra ceremony, for example) says so here.
// The reason is required: whitespace does not opt out, so the skip is a stated decision.
// The attribute goes on the contract class (the static class that holds the Query or Command). Matched BY SIMPLE NAME
// by the convention analyzers, which must not ProjectReference this assembly.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this contract has no mock response factory on purpose.
/// Suppresses TWA0046 only when the reason is non-empty and non-whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NoMockResponseAttribute : Attribute
{
  /// <summary>Why the SPA's mock service cannot answer this contract. Whitespace does not opt out.</summary>
  public string Reason { get; }

  /// <summary>Opts this contract out of TWA0046 when <paramref name="reason"/> is non-empty.</summary>
  public NoMockResponseAttribute(string reason)
  {
    Reason = reason;
  }
}
