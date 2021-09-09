namespace TimeWarp.Blazor.Data
{
  using Microsoft.EntityFrameworkCore;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  public class CosmosDbContext : DbContext
  {
    public CosmosDbContext(DbContextOptions<CosmosDbContext> aDbContextOptions) : base(aDbContextOptions) { }
  }
}
