namespace TimeWarp.Blazor.Features.Superheros
{
  using System.Threading.Tasks;
  using static TimeWarp.Blazor.Features.Superheros.SuperheroState;

  public partial class SuperheroPage
  {
    private const string RouteTemplate = "/Superhero";
    public static string GetRoute() => RouteTemplate;

    protected override async Task OnInitializedAsync() => await Send(new FetchSuperheroAction() { }).ConfigureAwait(false);
  }
}
