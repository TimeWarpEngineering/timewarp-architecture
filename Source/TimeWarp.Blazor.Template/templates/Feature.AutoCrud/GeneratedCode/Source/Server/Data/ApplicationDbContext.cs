namespace __RootNamespace__.Data
{
  using Microsoft.EntityFrameworkCore;

  public class ApplicationDbContext : DbContext
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<__FeatureName__Entity> __FeatureName__Entities { get; set; }
  }
}