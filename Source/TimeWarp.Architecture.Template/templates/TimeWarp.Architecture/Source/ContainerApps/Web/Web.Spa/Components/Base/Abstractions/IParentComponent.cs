namespace TimeWarp.Architecture.Components;

public interface IParentComponent
{
  [Parameter] public RenderFragment ChildContent { get; set; }
}
