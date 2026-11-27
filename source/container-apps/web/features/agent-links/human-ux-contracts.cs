#region Purpose
// Portable humanUx JSON an agent can present to its linked human (A2A-shaped handoff).
#endregion

#region Design
// Spec id `timewarp.humanUx/v1`. This is a document the agent shows its operator — not a
// payment prerequisite (locked 104 decision 3: no human required if the agent pays).
// Sample: source/container-apps/web/features/agent-links/human-ux.sample.json
//
// Schema (camelCase JSON):
//   spec        string  — "timewarp.humanUx/v1"
//   kind        string  — "handoff"
//   title       string  — short heading the agent can render
//   summary     string  — one-paragraph explanation
//   link        object  — { id, status, agentPrincipalId, humanPrincipalId }
//   human       object? — { displayName, email } optional chrome. GetHumanUx fills
//                         displayName from TimeWarp.Identity.Principal (other assembly).
//                         email is not read from Features.Profiles (TWA0009); the sample
//                         JSON shows the field for agents that already have it.
//   actions     array   — [{ id, label, href }] relative UI links the human can open
//
// GetHumanUx.Response is this document. Agents GET it only for an Approved link they own.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

public sealed class HumanUxLink
{
  /// <summary>Id of the link.</summary>
  public Guid Id { get; }
  /// <summary>Link status.</summary>
  public string Status { get; }
  /// <summary>Principal id of the agent.</summary>
  public Guid AgentPrincipalId { get; }
  /// <summary>Principal id of the human.</summary>
  public Guid HumanPrincipalId { get; }

  public HumanUxLink(Guid id, string status, Guid agentPrincipalId, Guid humanPrincipalId)
  {
    Id = Guard.Against.NullOrEmpty(id);
    Status = Guard.Against.NullOrEmpty(status);
    AgentPrincipalId = Guard.Against.NullOrEmpty(agentPrincipalId);
    HumanPrincipalId = Guard.Against.NullOrEmpty(humanPrincipalId);
  }
}

public sealed class HumanUxHuman
{
  /// <summary>Name shown for the principal.</summary>
  public string DisplayName { get; }
  /// <summary>Email address, when known.</summary>
  public string? Email { get; }

  public HumanUxHuman(string displayName, string? email)
  {
    DisplayName = Guard.Against.NullOrEmpty(displayName);
    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
  }
}

public sealed class HumanUxAction
{
  /// <summary>Id of the action.</summary>
  public string Id { get; }
  /// <summary>Text shown for the action.</summary>
  public string Label { get; }
  /// <summary>Relative URL of the action.</summary>
  public string Href { get; }

  public HumanUxAction(string id, string label, string href)
  {
    Id = Guard.Against.NullOrEmpty(id);
    Label = Guard.Against.NullOrEmpty(label);
    Href = Guard.Against.NullOrEmpty(href);
  }
}
