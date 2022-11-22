namespace TimeWarp.Architecture.Components;

public class ParentComponent : DisplayComponent, IParentComponent
{
  [Parameter] public RenderFragment ChildContent { get; set; }
}
