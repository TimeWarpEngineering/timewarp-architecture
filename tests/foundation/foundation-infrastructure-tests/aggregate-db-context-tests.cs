#region Purpose
// Automated coverage for AggregateDbContext: SaveChanges (Version increment, child→root gap,
// fail-closed missing Version) and the aggregate Version concurrency convention (task 121).
#endregion

namespace AggregateDbContext_;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeWarp.Foundation.Application.Exceptions;
using TimeWarp.Foundation.Entities;
using TimeWarp.Foundation.Persistence;

public class SaveChanges_Hook
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SaveChanges_Hook>();

  public static Task Root_only_modify_increments_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    db.Roots.Add(root);
    db.SaveChanges();
    root.Version.ShouldBe(0);

    root.Rename("beta");
    db.SaveChanges();

    root.Version.ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task Child_only_mutation_marks_root_modified_and_increments_version()
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
    return Task.CompletedTask;
  }

  public static Task Adding_owned_child_after_initial_save_increments_root_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    db.Roots.Add(root);
    db.SaveChanges();
    root.Version.ShouldBe(0);

    root.AddLine("second");
    db.ChangeTracker.DetectChanges();
    db.Entry(root).State.ShouldBe(EntityState.Unchanged);

    db.SaveChanges();

    root.Version.ShouldBe(1);
    root.Lines.Single().Text.ShouldBe("second");
    return Task.CompletedTask;
  }

  public static Task Deleting_owned_child_increments_root_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    root.AddLine("first");
    db.Roots.Add(root);
    db.SaveChanges();
    root.Version.ShouldBe(0);

    TestLine line = root.Lines.Single();
    db.Entry(line).State = EntityState.Deleted;
    db.ChangeTracker.DetectChanges();
    db.Entry(root).State.ShouldBe(EntityState.Unchanged);

    db.SaveChanges();

    root.Version.ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task Child_only_mutation_runs_root_invariants()
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
    return Task.CompletedTask;
  }

  public static Task Added_root_does_not_increment_version()
  {
    using HarnessDbContext db = CreateDb();
    TestRoot root = new(Guid.NewGuid(), "alpha");
    db.Roots.Add(root);

    db.SaveChanges();

    root.Version.ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Missing_version_on_modified_root_fails_closed()
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
    return Task.CompletedTask;
  }

  public static async Task Save_changes_async_increments_version_on_root_modify()
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

internal sealed class HarnessDbContext : AggregateDbContext
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

internal sealed class VersionlessDbContext : AggregateDbContext
{
  public VersionlessDbContext(DbContextOptions<VersionlessDbContext> options) : base(options) { }

  public DbSet<RootWithoutVersion> Roots => Set<RootWithoutVersion>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<RootWithoutVersion>().HasKey(root => root.Id);
  }
}

/// <summary>
/// Coverage for AggregateVersionConvention (task 121): every mapped IAggregateRoot gets
/// Version.IsConcurrencyToken + PropertyAccessMode.Property for free, with no host mapping call
/// and regardless of how the entity type reached the model.
/// </summary>
public class ConcurrencyConvention
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ConcurrencyConvention>();

  public static Task Root_without_explicit_mapping_gets_concurrency_token_and_access_mode()
  {
    using PlainRootDbContext db = CreatePlainRootDb();

    IEntityType entityType = db.Model.FindEntityType(typeof(PlainRoot))
      .ShouldNotBeNull("PlainRoot must be on the model even without an explicit mapping");

    IProperty version = entityType.FindProperty(nameof(Entity<Guid>.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
    return Task.CompletedTask;
  }

  public static Task Config_only_root_without_dbset_gets_concurrency_token_and_access_mode()
  {
    using ConfigOnlyDbContext db = CreateConfigOnlyDb();

    // ConfigOnlyRoot has no DbSet<> property — it is only discovered via ApplyConfiguration,
    // called AFTER base.OnModelCreating. This is exactly the ordering gap the old
    // OnModelCreating-loop pin could miss; the model-finalizing convention always sees it.
    IEntityType entityType = db.Model.FindEntityType(typeof(ConfigOnlyRoot))
      .ShouldNotBeNull("ConfigOnlyRoot must be on the model via ApplyConfiguration alone");

    IProperty version = entityType.FindProperty(nameof(Entity<Guid>.Version)).ShouldNotBeNull();
    version.IsConcurrencyToken.ShouldBeTrue();
    version.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Property);
    return Task.CompletedTask;
  }

  private static PlainRootDbContext CreatePlainRootDb()
  {
    DbContextOptions<PlainRootDbContext> options = new DbContextOptionsBuilder<PlainRootDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new PlainRootDbContext(options);
  }

  private static ConfigOnlyDbContext CreateConfigOnlyDb()
  {
    DbContextOptions<ConfigOnlyDbContext> options = new DbContextOptionsBuilder<ConfigOnlyDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new ConfigOnlyDbContext(options);
  }
}

/// <summary>IAggregateRoot mapped with no explicit Version configuration at all.</summary>
internal sealed class PlainRoot : Entity<Guid>, IAggregateRoot
{
  public PlainRoot(Guid id, string name) : base(id)
  {
    Name = name;
  }

  public string Name { get; private set; }

  private sealed class Invariants : AbstractValidator<PlainRoot>
  {
    public Invariants()
    {
      RuleFor(root => root.Name).NotEmpty();
    }
  }
}

/// <summary>
/// HasKey is the only explicit mapping call — required so EF's constructor-binding convention can
/// bind the base class's get-only Id property (an EF requirement independent of task 121). No
/// IsConcurrencyToken/UsePropertyAccessMode call anywhere: proving those come from the convention.
/// </summary>
internal sealed class PlainRootDbContext : AggregateDbContext
{
  public PlainRootDbContext(DbContextOptions<PlainRootDbContext> options) : base(options) { }

  public DbSet<PlainRoot> Roots => Set<PlainRoot>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<PlainRoot>(entity => entity.HasKey(root => root.Id));
  }
}

/// <summary>IAggregateRoot discovered ONLY via ApplyConfiguration — deliberately no DbSet property.</summary>
internal sealed class ConfigOnlyRoot : Entity<Guid>, IAggregateRoot
{
  public ConfigOnlyRoot(Guid id, string name) : base(id)
  {
    Name = name;
  }

  public string Name { get; private set; }

  private sealed class Invariants : AbstractValidator<ConfigOnlyRoot>
  {
    public Invariants()
    {
      RuleFor(root => root.Name).NotEmpty();
    }
  }
}

internal sealed class ConfigOnlyRootConfiguration : IEntityTypeConfiguration<ConfigOnlyRoot>
{
  public void Configure(EntityTypeBuilder<ConfigOnlyRoot> builder)
  {
    builder.HasKey(root => root.Id);
  }
}

internal sealed class ConfigOnlyDbContext : AggregateDbContext
{
  public ConfigOnlyDbContext(DbContextOptions<ConfigOnlyDbContext> options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // No DbSet<ConfigOnlyRoot> property on this context — ApplyConfiguration after base is the
    // only path that adds it to the model, mirroring a config-only aggregate in a host context.
    modelBuilder.ApplyConfiguration(new ConfigOnlyRootConfiguration());
  }
}
