namespace TimeWarp.Architecture.Components;

public partial class Transition
{
  [Parameter] public RenderFragment ChildContent { get; set; }

  [Parameter] public bool Show { get; set; }

  /// <summary>
  /// enter: Applied the entire time an element is entering.
  /// Usually you define your duration and what properties you want to transition here, for example transition-opacity duration-75.
  /// </summary>
  [Parameter] public string Enter { get; set; }

  /// <summary>
  /// enterFrom: The starting point to enter from, for example opacity-0 if something should fade in.
  /// </summary>
  [Parameter] public string EnterFrom { get; set; }

  /// <summary>
  /// enterTo: The ending point to enter to, for example opacity-100 after fading in.
  /// </summary>
  /// <example>
  /// </example>
  [Parameter] public string EnterTo { get; set; }

  /// <summary>
  /// leave: Applied the entire time an element is leaving.
  /// Usually you define your duration and what properties you want to transition here.
  /// </summary>
  /// <example>
  /// transition-opacity duration-75
  /// </example>
  [Parameter] public string Leave { get; set; }

  /// <summary>
  /// leaveFrom: The starting point to leave from, for example opacity-100 if something should fade out.
  /// </summary>
  [Parameter] public string LeaveFrom { get; set; }

  /// <summary>
  /// leaveTo: The ending point to leave to, for example opacity-0 after fading out.
  /// </summary>
  [Parameter] public string LeaveTo { get; set; }

}
