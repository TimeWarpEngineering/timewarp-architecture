namespace TimeWarp.Architecture.Configuration;

using static RenderMode;
public sealed class BlazorSettings
{
  // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
  public RenderMode RenderMode { get; init; } = InteractiveAuto;
  public bool Prerender { get; init; } = true;
}

public enum RenderMode
{
  InteractiveServer,
  InteractiveWebAssembly,
  InteractiveAuto
}

[UsedImplicitly]
internal sealed class BlazorSettingsValidator: AbstractValidator<BlazorSettings>;
