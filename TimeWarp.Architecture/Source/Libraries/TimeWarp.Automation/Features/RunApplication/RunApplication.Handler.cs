namespace TimeWarp.Automation.Features.Application;

using static TimeWarp.Automation.Features.RunApplication;

public class Handler : IRequestHandler<Command, OneOf<Response, ValidationResult, Exception>>
{
  public Task<OneOf<Response, ValidationResult, Exception>> Handle
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
        UseShellExecute = true
      };

      Process process = Process.Start(startInfo)
        ?? throw new Exception($"Failed to start process: {command.ApplicationPath}");

      var response = new Response
      {
        ProcessId = process.Id
      };

      return Task.FromResult<OneOf<Response, ValidationResult, Exception>>(response);
    }
    catch (Exception ex)
    {
      return Task.FromResult<OneOf<Response, ValidationResult, Exception>>(ex);
    }
  }
}
