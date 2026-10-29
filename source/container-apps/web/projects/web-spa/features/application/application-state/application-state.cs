#region Purpose
// Application shell UI state: active modal, nav width choice, and displayed version.
#endregion

#region Design
// A single ActiveModalId gates all modals, so at most one is open and components carry no
// per-modal open/closed flags.
// Version is derived from the assembly rather than stored, so it cannot drift from the
// deployed build.
// Setters are private; mutation happens only via action-set handlers in sibling partials.
// The product name and the logo are BrandOptions. This store does not keep a second copy.
// HasShellNavChoice false means the nav follows the breakpoint. IsMenuExpanded is the explicit
// choice once ToggleMenu or LoadShellNav has one: true is the full column, false is the rail.
// IsAssemblyInfoCopied is the version dialog's Copy confirmation (CopyAssemblyInfo).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>
/// Represents application-level UI state shared across the Blazor client.
/// </summary>
/// <remarks>
/// The state tracks the active modal, menu expansion, and assembly version displayed by the application shell.
/// <see cref="Initialize()"/> sets the default shell values used when the store starts.
/// </remarks>
[StateAccess]
public sealed partial class ApplicationState : State<ApplicationState>
{
  /// <summary>
  /// Gets the identifier of the active modal, or <c>null</c> when no modal is active.
  /// </summary>
  public string? ActiveModalId { get; private set; }

  /// <summary>
  /// Gets a value indicating whether the explicit nav choice is the full column.
  /// Meaningful once <see cref="HasShellNavChoice"/> is true; otherwise the breakpoint decides.
  /// </summary>
  public bool IsMenuExpanded { get; private set; }

  /// <summary>
  /// Gets a value indicating whether this browser has stored a nav width.
  /// False follows the breakpoint: full from 1280px, rail from 880px through 1279px.
  /// </summary>
  public bool HasShellNavChoice { get; private set; }

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
    HasShellNavChoice = false;
    IsAssemblyInfoCopied = false;
  }
}
