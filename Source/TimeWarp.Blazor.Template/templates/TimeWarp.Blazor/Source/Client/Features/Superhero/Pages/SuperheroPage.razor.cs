﻿namespace TimeWarp.Blazor.Pages;

using System.Threading.Tasks;
using TimeWarp.Blazor.Features.Bases;
using static TimeWarp.Architecture.Features.Superheros.SuperheroState;

public partial class SuperheroPage : BaseComponent
{
  private const string RouteTemplate = "/Superheros";
  public static string GetRoute() => RouteTemplate;

  protected override async Task OnInitializedAsync() => await Send(new FetchSuperheroAction()).ConfigureAwait(false);
}
