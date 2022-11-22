namespace TimeWarp.Architecture.Data;

public class CosmosDbContext : DbContext
{
  public DbSet<Profile> Profiles => Set<Profile>();

  public CosmosDbContext(DbContextOptions<CosmosDbContext> aDbContextOptions) : base(aDbContextOptions) { }

  protected override void OnModelCreating(ModelBuilder aModelBuilder)
  {
    aModelBuilder.ApplyConfiguration<Profile>(new ProfileConfiguration());
    base.OnModelCreating(aModelBuilder);
  }
}
