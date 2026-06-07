namespace TimeWarp.Architecture.Data;

public class CosmosDbContext : DbContext
{
  public DbSet<Profile> Profiles => Set<Profile>();

  public CosmosDbContext(DbContextOptions<CosmosDbContext> dbContextOptions) : base(dbContextOptions) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration<Profile>(new ProfileConfiguration());
    base.OnModelCreating(modelBuilder);
  }
}
