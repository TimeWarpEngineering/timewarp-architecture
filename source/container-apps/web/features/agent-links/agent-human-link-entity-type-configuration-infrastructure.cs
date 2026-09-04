#region Purpose
// EF Core mapping for AgentHumanLink: schema/table, TypedId key, Version concurrency token.
#endregion

#region Design
// Schema-per-slice on the single PostgresDbContext: table and PostgreSQL schema are both
// "agent_links". Version IsConcurrencyToken is supplied by AggregateVersionConvention.
// PrincipalId conversions are explicit exemplars (same as PrincipalEntityTypeConfiguration).
// Pair uniqueness is a filtered unique index on Pending/Approved only — Denied rows may
// repeat so the same agent+human pair can be requested again after denial.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Infrastructure;

using TimeWarp.Architecture.Features.AgentLinks.Domain;

public sealed class AgentHumanLinkEntityTypeConfiguration : IEntityTypeConfiguration<AgentHumanLink>
{
  public const string SchemaName = "agent_links";
  public const string TableName = "agent_links";

  public void Configure(EntityTypeBuilder<AgentHumanLink> builder)
  {
    builder.ToTable(TableName, SchemaName);

    builder.HasKey(link => link.Id);

    builder.Property(link => link.Id)
      .HasConversion(
        id => id.Value,
        value => AgentHumanLinkId.From(value))
      .ValueGeneratedNever();

    builder.Property(link => link.AgentPrincipalId).IsRequired();
    builder.Property(link => link.HumanPrincipalId).IsRequired();

    builder.Property(link => link.Status).IsRequired();
    builder.Property(link => link.CreatedAt).IsRequired();
    builder.Property(link => link.DecidedAt);

    builder.Property(link => link.Version)
      .UsePropertyAccessMode(PropertyAccessMode.Property);

    builder.HasIndex(link => new { link.AgentPrincipalId, link.HumanPrincipalId })
      .IsUnique()
      .HasFilter($"\"Status\" IN ({(int)AgentHumanLinkStatus.Pending}, {(int)AgentHumanLinkStatus.Approved})");
  }
}
