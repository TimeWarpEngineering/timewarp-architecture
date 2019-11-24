namespace TimeWarp.Blazor.Client.Components
{
  using Microsoft.AspNetCore.Components;

  public partial class SurveyPrompt
  {
    [Parameter]
    public string Title { get; set; } // Demonstrates how a parent component can supply parameters
  }
}
