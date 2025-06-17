namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Superheros.SuperheroState;

[Page("/Superheros")]
partial class SuperheroPage
{
  protected override async Task OnInitializedAsync() => await SuperheroState.FetchSuperhero();
}
