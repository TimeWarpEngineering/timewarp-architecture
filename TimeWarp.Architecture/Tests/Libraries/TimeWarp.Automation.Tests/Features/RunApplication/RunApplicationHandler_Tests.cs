namespace RunApplicationHandler;

using static TimeWarp.Automation.Features.RunApplication;

public class Handle_Returns
{
  private readonly Command Command;
  private readonly Features.Application.Handler Handler;

  public Handle_Returns()
  {
    Command = new Command { ApplicationPath = "notepad.exe" };
    Handler = new Features.Application.Handler();
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
        response.Should().NotBeNull();
        response.ProcessId.Should().BeGreaterThan(0);

        // Cleanup
        var process = Process.GetProcessById(response.ProcessId);
        process.Kill();
      },
      validationResult =>
      {
        Execute.Assertion.FailWith("The RunApplication handler returned ValidationResult instead of a successful response.");
      },
      exception =>
      {
        Execute.Assertion.FailWith("The RunApplication handler returned Exception instead of a successful response.");
      }
    );
  }
}
