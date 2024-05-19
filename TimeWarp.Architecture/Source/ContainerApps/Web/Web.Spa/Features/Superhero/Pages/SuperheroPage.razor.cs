namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Superheros.SuperheroState;

[Page("/Superheros")]
public partial class SuperheroPage : BaseComponent
{
  protected override async Task OnInitializedAsync() => await Send(new FetchSuperhero.Action());
}
