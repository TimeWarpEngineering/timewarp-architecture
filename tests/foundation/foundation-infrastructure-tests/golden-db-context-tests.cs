#region Purpose
// Automated coverage for GoldenDbContext SaveChanges: Version increment, child→root gap, fail-closed missing Version.
#endregion

namespace GoldenDbContext_;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TimeWarp.Foundation.Application.Exceptions;
using TimeWarp.Foundation.Entities;
using TimeWarp.Foundation.Persistence;

public class SaveChanges_Hook
{
  public void Root_only_modify_increments_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    db.Roots.Add(root);
    db.SaveChanges();
    root.Version.ShouldBe(0);

    root.Rename("beta");
    db.SaveChanges();

    root.Version.ShouldBe(1);
  }

  public void Child_only_mutation_marks_root_modified_and_increments_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    root.AddLine("first");
    db.Roots.Add(root);
    db.SaveChanges();
    root.Version.ShouldBe(0);

    // Mutate only the owned child — root scalar properties stay unchanged.
    root.RewriteFirstLine("first-rewritten");
    db.ChangeTracker.DetectChanges();
    EntityState rootStateBeforeSave = db.Entry(root).State;
    rootStateBeforeSave.ShouldBe(EntityState.Unchanged);

    db.SaveChanges();

    root.Version.ShouldBe(1);
    root.Lines.Single().Text.ShouldBe("first-rewritten");
  }

  public void Child_only_mutation_runs_root_invariants()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    root.AddLine("first");
    db.Roots.Add(root);
    db.SaveChanges();

    root.BlankFirstLine();
    long versionBefore = root.Version;

    Should.Throw<DomainInvariantViolationException>(() => db.SaveChanges());
    root.Version.ShouldBe(versionBefore);
  }

  public void Added_root_does_not_increment_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    db.Roots.Add(root);

    db.SaveChanges();

    root.Version.ShouldBe(0);
  }

  public void Missing_version_on_modified_root_fails_closed()
  {
    using VersionlessDbContext db = CreateVersionlessDb();
    RootWithoutVersion root = new() { Id = Guid.NewGuid(), Name = "alpha" };
    db.Roots.Add(root);
    db.SaveChanges();

    root.Name = "beta";
    db.Entry(root).State = EntityState.Modified;

    InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => db.SaveChanges());
    ex.Message.ShouldContain(nameof(IAggregateRoot));
    ex.Message.ShouldContain(nameof(Entity<Guid>.Version));
  }

  public async Task Save_changes_async_increments_version_on_root_modify()
  {
    await using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    db.Roots.Add(root);
    await db.SaveChangesAsync();

    root.Rename("beta");
    await db.SaveChangesAsync();

    root.Version.ShouldBe(1);
  }

  private static HarnessDbContext CreateDb()
  {
    DbContextOptions<HarnessDbContext> options = new DbContextOptionsBuilder<HarnessDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new HarnessDbContext(options);
  }

  private static VersionlessDbContext CreateVersionlessDb()
  {
    DbContextOptions<VersionlessDbContext> options = new DbContextOptionsBuilder<VersionlessDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new VersionlessDbContext(options);
  }
}

/// <summary>Test-local aggregate with owned children for the child→root gap.</summary>
internal sealed class TestRoot : Entity<Guid>, IAggregateRoot
{
  public TestRoot(Guid id, string name) : base(id)
  {
    Name = name;
  }

  public string Name { get; private set; }

  public List<TestLine> Lines { get; private set; } = [];

  public void Rename(string name) => Name = name;

  public void AddLine(string text) => Lines.Add(new TestLine(text));

  public void RewriteFirstLine(string text) => Lines[0].Rewrite(text);

  public void BlankFirstLine() => Lines[0].Blank();

  private sealed class Invariants : AbstractValidator<TestRoot>
  {
    public Invariants()
    {
      RuleFor(root => root.Name).NotEmpty();
      RuleForEach(root => root.Lines).ChildRules
      (
        line => line.RuleFor(item => item.Text).NotEmpty()
      );
    }
  }
}

internal sealed class TestLine
{
  public TestLine(string text)
  {
    Text = text;
  }

  public string Text { get; private set; }

  public void Rewrite(string text) => Text = text;

  public void Blank() => Text = string.Empty;
}

/// <summary>IAggregateRoot that deliberately omits Entity{TId}/Version for the fail-closed path.</summary>
internal sealed class RootWithoutVersion : IAggregateRoot
{
  public Guid Id { get; set; }

  public string Name { get; set; } = null!;

  private sealed class Invariants : AbstractValidator<RootWithoutVersion>
  {
    public Invariants()
    {
      RuleFor(root => root.Name).NotEmpty();
    }
  }
}

internal sealed class HarnessDbContext : GoldenDbContext
{
  public HarnessDbContext(DbContextOptions<HarnessDbContext> options) : base(options) { }

  public DbSet<TestRoot> Roots => Set<TestRoot>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<TestRoot>
    (
      entity =>
      {
        entity.HasKey(root => root.Id);
        entity.Property(root => root.Version).IsConcurrencyToken();
        entity.OwnsMany
        (
          root => root.Lines,
          owned =>
          {
            owned.WithOwner().HasForeignKey("RootId");
            owned.Property<int>("Id");
            owned.HasKey("Id");
            owned.Property(line => line.Text).IsRequired();
          }
        );
      }
    );
  }
}

internal sealed class VersionlessDbContext : GoldenDbContext
{
  public VersionlessDbContext(DbContextOptions<VersionlessDbContext> options) : base(options) { }

  public DbSet<RootWithoutVersion> Roots => Set<RootWithoutVersion>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<RootWithoutVersion>().HasKey(root => root.Id);
  }
}
