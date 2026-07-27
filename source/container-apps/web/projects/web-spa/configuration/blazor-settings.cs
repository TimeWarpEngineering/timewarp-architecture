#region Purpose
// Configuration-bound choice of Blazor render mode and prerendering for the whole app.
#endregion

#region Design
// A local RenderMode enum (not the framework's IComponentRenderMode types) keeps the setting
// bindable from appsettings as a plain string; the host maps it to real render modes.
// The empty validator keeps the FluentValidation options-validation registration uniform with
// other settings classes.
#endregion

namespace TimeWarp.Architecture.Configuration;

using static RenderMode;
public sealed class BlazorSettings
{
  public RenderMode RenderMode { get; init; } = InteractiveAuto;
  public bool Prerender { get; init; } = true;
}

public enum RenderMode
{
  InteractiveServer,
  InteractiveWebAssembly,
  InteractiveAuto
}

internal sealed class BlazorSettingsValidator: AbstractValidator<BlazorSettings>;
