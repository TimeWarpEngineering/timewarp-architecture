#nullable enable
namespace TimeWarp.Architecture.Components;

public abstract class ParentComponent : DisplayComponent, IParentComponent
{
  [Parameter] public RenderFragment ChildContent { get; set; } = null!;
}
