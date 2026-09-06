#region Purpose
// Lifecycle of an optional Agent ↔ Human link: pending until the human approves or denies.
#endregion

#region Design
// Reserved zero (None) so default/missing values fail closed at domain entry. Paid service
// (metered capability / x402) never consults this status — locked 104 decision 3.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Domain;

public enum AgentHumanLinkStatus
{
  None = 0,
  Pending = 1,
  Approved = 2,
  Denied = 3
}
