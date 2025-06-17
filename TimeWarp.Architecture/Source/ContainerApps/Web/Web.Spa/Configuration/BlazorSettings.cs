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
