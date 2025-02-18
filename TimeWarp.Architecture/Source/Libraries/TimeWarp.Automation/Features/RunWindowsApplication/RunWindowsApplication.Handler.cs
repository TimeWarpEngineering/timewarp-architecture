namespace TimeWarp.Automation.Features.WindowsApplication;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static TimeWarp.Automation.Features.RunWindowsApplication;

[SupportedOSPlatform("windows")]
public class Handler : IRequestHandler<Command, OneOf<Response, ValidationResult, Exception>>
{
  public async Task<OneOf<Response, ValidationResult, Exception>> Handle
  (
    Command command,
    CancellationToken cancellationToken
  )
  {
    try
    {
      ProcessStartInfo startInfo = new()
      {
        FileName = command.ApplicationPath,
        Arguments = command.Arguments,
        UseShellExecute = true,
        WorkingDirectory = command.WorkingDirectory ?? Path.GetDirectoryName(command.ApplicationPath) ?? string.Empty,
      };

      // Map WindowStyle to ProcessWindowStyle
      startInfo.WindowStyle = command.WindowStyle switch
      {
        WindowStyle.Normal => ProcessWindowStyle.Normal,
        WindowStyle.Hidden => ProcessWindowStyle.Hidden,
        WindowStyle.Minimized => ProcessWindowStyle.Minimized,
        WindowStyle.Maximized => ProcessWindowStyle.Maximized,
        _ => ProcessWindowStyle.Normal
      };

      Process process = Process.Start(startInfo)
        ?? throw new Exception($"Failed to start process: {command.ApplicationPath}");

      if (command.AfterLaunch == AfterLaunchBehavior.WaitForApplicationToLoad)
      {
        // Wait for window handle to be available
        TimeSpan timeout = command.Timeout ?? TimeSpan.FromSeconds(30);
        DateTime startTime = DateTime.UtcNow;

        while (process.MainWindowHandle == IntPtr.Zero)
        {
          if (DateTime.UtcNow - startTime > timeout)
          {
            throw new TimeoutException($"Timeout waiting for window handle after {timeout.TotalSeconds} seconds");
          }

          await Task.Delay(100, cancellationToken);
          process.Refresh();
        }
      }

      var response = new Response
      {
        ProcessId = process.Id,
        WindowHandle = process.MainWindowHandle
      };

      return response;
    }
    catch (Exception ex)
    {
      return ex;
    }
  }
}
