namespace RunWindowsApplicationHandler;

using System.Runtime.Versioning;
using static TimeWarp.Automation.Features.RunWindowsApplication;
using TimeWarp.Automation.Features.WindowsApplication;

[SupportedOSPlatform("windows")]
public class Handle_Returns
{
  private readonly Handler Handler;
  private readonly string NotePadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");

  public Handle_Returns()
  {
    Handler = new Handler();
  }

  public async Task ProcessIdAndWindowHandle_Given_ValidApplication()
  {
    // Arrange
    Command command = new()
    {
      ApplicationPath = NotePadPath,
      WindowStyle = WindowStyle.Normal,
      AfterLaunch = AfterLaunchBehavior.WaitForApplicationToLoad
    };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    ValidateSuccessResult(result);
  }

  public async Task ProcessIdAndWindowHandle_Given_MinimizedWindow()
  {
    // Arrange
    Command command = new()
    {
      ApplicationPath = NotePadPath,
      WindowStyle = WindowStyle.Minimized,
      AfterLaunch = AfterLaunchBehavior.WaitForApplicationToLoad
    };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    ValidateSuccessResult(result, validateWindowStyle: true);
  }

  public async Task TimeoutException_Given_NoWindowHandle()
  {
    // Arrange
    Command command = new()
    {
      ApplicationPath = "cmd.exe",
      Arguments = "/c exit",  // Command that exits immediately
      Timeout = TimeSpan.FromSeconds(1),
      AfterLaunch = AfterLaunchBehavior.WaitForApplicationToLoad
    };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsT2.ShouldBeTrue();
    result.AsT2.ShouldBeOfType<TimeoutException>();
  }

  public async Task ProcessId_Given_ContinueImmediately()
  {
    // Arrange
    Command command = new()
    {
      ApplicationPath = "cmd.exe",
      Arguments = "/c exit",
      AfterLaunch = AfterLaunchBehavior.ContinueImmediately
    };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    ValidateSuccessResult(result, validateWindowHandle: false);
  }

  private void ValidateSuccessResult
  (
    OneOf<Response, ValidationResult, Exception> result,
    bool validateWindowHandle = true,
    bool validateWindowStyle = false
  )
  {
    result.Switch
    (
      response =>
      {
        response.ShouldNotBeNull();
        response.ProcessId.ShouldBeGreaterThan(0);

        if (validateWindowHandle)
        {
          response.WindowHandle.ShouldNotBe(IntPtr.Zero);
        }

        if (validateWindowStyle)
        {
          var windowProcess = Process.GetProcessById(response.ProcessId);
          windowProcess.MainWindowHandle.ShouldNotBe(IntPtr.Zero);
        }

        // Cleanup
        var process = Process.GetProcessById(response.ProcessId);
        process.Kill();
      },
      validationResult =>
      {
        throw new ShouldAssertException("The RunWindowsApplication handler returned ValidationResult instead of a successful response.");
      },
      exception =>
      {
        throw new ShouldAssertException("The RunWindowsApplication handler returned Exception instead of a successful response.");
      }
    );
  }
}
