namespace TimeWarp.Architecture.Components;

public interface IParentComponent
{
  [Parameter] RenderFragment ChildContent { get; set; }
}
