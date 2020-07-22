namespace TimeWarp.Blazor.Components
{
  using Microsoft.AspNetCore.Components;

  public partial class Box : DisplayComponent
  {
    [Parameter] public RenderFragment ChildContent { get; set; }
  }
}
