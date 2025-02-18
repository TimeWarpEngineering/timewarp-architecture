namespace RunApplicationHandler;

using static TimeWarp.Automation.Features.RunApplication;
using TimeWarp.Automation.Features.Application;

public class Handle_Returns
{
  private readonly Command Command;
  private readonly Handler Handler;

  public Handle_Returns()
  {
    Command = new Command { ApplicationPath = "notepad.exe" };
    Handler = new Handler();
  }

  public async Task ProcessId_Given_ValidApplication()
  {
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(Command, CancellationToken.None);

    ValidateResult(result);
  }

  private void ValidateResult(OneOf<Response, ValidationResult, Exception> result)
  {
    result.Switch
    (
      response =>
      {
        response.ShouldNotBeNull();
        response.ProcessId.ShouldBeGreaterThan(0);

        // Cleanup
        var process = Process.GetProcessById(response.ProcessId);
        process.Kill();
      },
      validationResult =>
      {
        throw new ShouldAssertException("The RunApplication handler returned ValidationResult instead of a successful response.");
      },
      exception =>
      {
        throw new ShouldAssertException("The RunApplication handler returned Exception instead of a successful response.");
      }
    );
  }
}
