namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  public class SuperheroGrpcDto
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public Powerstats Powerstats { get; set; }
    public Image Image { get; set; }

  }

  public class Powerstats
  {
    public string Intelligence { get; set; }
    public string Strength { get; set; }
    public string Speed { get; set; }
    public string Durability { get; set; }
  }
  public class Image
  {
    public string Url { get; set; }
  }
}
