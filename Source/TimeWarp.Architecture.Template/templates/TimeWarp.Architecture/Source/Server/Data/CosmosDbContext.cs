namespace TimeWarp.Architecture.Data
{
  using Microsoft.EntityFrameworkCore;
  using TimeWarp.Architecture.Data.Configuration;
  using TimeWarp.Architecture.Entities;

  public class CosmosDbContext : DbContext
  {
    public DbSet<Profile> Profiles { get; set; }

    public CosmosDbContext(DbContextOptions<CosmosDbContext> aDbContextOptions) : base(aDbContextOptions) { }

    protected override void OnModelCreating(ModelBuilder aModelBuilder)
    {
      aModelBuilder.ApplyConfiguration<Profile>(new ProfileConfiguration());
      base.OnModelCreating(aModelBuilder);
    }
  }
}
