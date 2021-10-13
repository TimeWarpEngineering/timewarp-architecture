namespace TimeWarp.Blazor.Data
{
  using Microsoft.EntityFrameworkCore;
  using TimeWarp.Blazor.Data.Configuration;
  using TimeWarp.Blazor.Entities;

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
