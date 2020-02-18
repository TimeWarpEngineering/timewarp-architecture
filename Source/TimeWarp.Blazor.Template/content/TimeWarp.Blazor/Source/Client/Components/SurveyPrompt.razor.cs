namespace TimeWarp.Blazor.Components
{
  using Microsoft.AspNetCore.Components;
  using TimeWarp.Blazor.Features.Bases.Client;

  public partial class SurveyPrompt: BaseComponent
  {
    [Parameter]
    public string Title { get; set; } // Demonstrates how a parent component can supply parameters
  }
}
