namespace TimeWarp.Architecture.Features.Applications;

/// <summary>
/// Represents application-level UI state shared across the Blazor client.
/// </summary>
/// <remarks>
/// The state tracks the active modal, menu expansion, branding, and assembly version displayed by the application shell.
/// <see cref="Initialize"/> sets the default shell values used when the store starts.
/// </remarks>
[StateAccessMixin]
public sealed partial class ApplicationState : State<ApplicationState>
{
  /// <summary>
  /// Gets the identifier of the active modal, or <c>null</c> when no modal is active.
  /// </summary>
  public string? ActiveModalId { get; private set; }

  /// <summary>
  /// Gets a value indicating whether the application menu is expanded.
  /// </summary>
  public bool IsMenuExpanded { get; private set; }

  /// <summary>
  /// Gets the path to the application logo.
  /// </summary>
  public string? Logo { get; private set; }

  /// <summary>
  /// Gets the application display name.
  /// </summary>
  public string Name { get; private set; } = null!;

  /// <summary>
  /// Gets the version of the assembly containing this state type.
  /// </summary>
  public string? Version => GetType().Assembly.GetName().Version?.ToString();

  public ApplicationState() { }

  /// <summary>
  /// Sets the default application shell state.
  /// </summary>
  public override void Initialize()
  {
    IsMenuExpanded = true;
    Name = "TimeWarp.Architecture";
    Logo = "/images/logo.png";
  }
}
