#region Purpose
// Domain aggregate for an optional Agent ↔ Human link the human must approve.
#endregion

#region Design
// Product slice (task 205), not TimeWarp.Identity: the identity kernel stays principals,
// credentials, sessions, and tokens. A link is higher-level product — agents may pay and
// call without a human (locked 104 decision 3). Create starts Pending; Approve/Deny are the
// only transitions and only from Pending. Agent and human PrincipalIds are immutable after
// Create and must be distinct non-empty ids.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Domain;

using FluentValidation;

public sealed class AgentHumanLink : Entity<AgentHumanLinkId>, IAggregateRoot
{
  private AgentHumanLink(
    AgentHumanLinkId id,
    Guid agentPrincipalId,
    Guid humanPrincipalId,
    AgentHumanLinkStatus status,
    DateTimeOffset createdAt,
    DateTimeOffset? decidedAt)
    : base(id)
  {
    AgentPrincipalId = agentPrincipalId;
    HumanPrincipalId = humanPrincipalId;
    Status = status;
    CreatedAt = createdAt;
    DecidedAt = decidedAt;
  }

  public Guid AgentPrincipalId { get; }
  public Guid HumanPrincipalId { get; }
  public AgentHumanLinkStatus Status { get; private set; }
  public DateTimeOffset CreatedAt { get; }
  public DateTimeOffset? DecidedAt { get; private set; }

  public static AgentHumanLink Create(Guid agentPrincipalId, Guid humanPrincipalId)
  {
    if (agentPrincipalId == Guid.Empty)
    {
      throw new ArgumentException("AgentPrincipalId must be non-empty.", nameof(agentPrincipalId));
    }

    if (humanPrincipalId == Guid.Empty)
    {
      throw new ArgumentException("HumanPrincipalId must be non-empty.", nameof(humanPrincipalId));
    }

    if (agentPrincipalId == humanPrincipalId)
    {
      throw new ArgumentException("Agent and human principal ids must differ.", nameof(humanPrincipalId));
    }

    return new AgentHumanLink(
      AgentHumanLinkId.New(),
      agentPrincipalId,
      humanPrincipalId,
      AgentHumanLinkStatus.Pending,
      DateTimeOffset.UtcNow,
      decidedAt: null);
  }

  public void Approve()
  {
    EnsurePending();
    Status = AgentHumanLinkStatus.Approved;
    DecidedAt = DateTimeOffset.UtcNow;
  }

  public void Deny()
  {
    EnsurePending();
    Status = AgentHumanLinkStatus.Denied;
    DecidedAt = DateTimeOffset.UtcNow;
  }

  private void EnsurePending()
  {
    if (Status != AgentHumanLinkStatus.Pending)
    {
      throw new InvalidOperationException($"Link '{Id}' is {Status} and cannot be decided again.");
    }
  }

  private sealed class Invariants : AbstractValidator<AgentHumanLink>
  {
    public Invariants()
    {
      RuleFor(link => link.AgentPrincipalId).NotEmpty();
      RuleFor(link => link.HumanPrincipalId).NotEmpty();
      RuleFor(link => link.Status).IsInEnum().NotEqual(AgentHumanLinkStatus.None);
      RuleFor(link => link.HumanPrincipalId)
        .Must((link, humanId) => humanId != link.AgentPrincipalId);
    }
  }
}
