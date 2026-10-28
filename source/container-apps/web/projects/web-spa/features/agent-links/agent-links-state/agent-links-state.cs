#region Purpose
// State caching the signed-in principal's agent-human links for the Agent Links page.
#endregion

#region Design
// Population only through Fetch/Approve/Deny action-set partials. Initialize clears so sign-out
// does not leave another principal's pending approvals or a copied-slot flag on screen.
// CopyOnboarding records which onboarding button last wrote the clipboard.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

[StateAccess]
public sealed partial class AgentLinksState : State<AgentLinksState>
{
  public IReadOnlyList<ListAgentHumanLinks.LinkSummary> Items { get; private set; } = [];

  public override void Initialize()
  {
    Items = [];
    CopiedSlot = null;
  }
}
