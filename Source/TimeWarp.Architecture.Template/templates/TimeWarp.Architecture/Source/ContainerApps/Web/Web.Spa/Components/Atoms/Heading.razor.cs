namespace TimeWarp.Architecture.Components;

using Microsoft.AspNetCore.Components;

public partial class Heading
{
  [Parameter] public RenderFragment ChildContent { get; set; }
}
