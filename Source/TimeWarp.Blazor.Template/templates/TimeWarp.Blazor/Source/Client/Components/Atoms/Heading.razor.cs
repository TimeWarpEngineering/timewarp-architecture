namespace TimeWarp.Blazor.Components
{
  using Microsoft.AspNetCore.Components;

  public partial class Heading
  {
    [Parameter] public RenderFragment ChildContent { get; set; }
  }
}
