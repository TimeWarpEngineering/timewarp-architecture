namespace TimeWarp.Automation.Features;

using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public static partial class RunWindowsApplication
{
  public enum WindowStyle
  {
    Normal,
    Hidden,
    Minimized,
    Maximized
  }

  public enum AfterLaunchBehavior
  {
    WaitForApplicationToLoad,
    ContinueImmediately
  }

  public sealed partial class Command : IRequest<OneOf<Response, ValidationResult, Exception>>
  {
    public string ApplicationPath { get; set; } = null!;
    public string? Arguments { get; set; }
    public WindowStyle WindowStyle { get; set; } = WindowStyle.Normal;
    public string? WorkingDirectory { get; set; }
    public TimeSpan? Timeout { get; set; }
    public AfterLaunchBehavior AfterLaunch { get; set; } = AfterLaunchBehavior.WaitForApplicationToLoad;
  }

  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.ApplicationPath)
        .NotEmpty();

      RuleFor(command => command.Timeout)
        .Must(timeout => timeout == null || timeout.Value.TotalMilliseconds > 0)
        .WithMessage("Timeout must be positive");
    }
  }

  public sealed class Response
  {
    public int ProcessId { get; init; }
    public IntPtr WindowHandle { get; init; }
  }
}
