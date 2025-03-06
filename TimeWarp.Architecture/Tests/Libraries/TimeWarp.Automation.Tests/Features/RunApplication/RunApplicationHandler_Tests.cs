namespace RunApplicationHandler;

using static TimeWarp.Automation.Features.RunApplication;
using TimeWarp.Automation.Features.Application;

public class Handle_Returns
{
  private readonly Handler Handler;

  public Handle_Returns()
  {
    Handler = new Handler();
  }

  public async Task ProcessId_Given_ValidApplication()
  {
    // Arrange
    var command = new Command { ApplicationPath = "notepad.exe" };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    ValidateSuccessResult(result);
  }

  public async Task ProcessId_Given_ValidApplication_With_Arguments()
  {
    // Arrange
    var command = new Command
    {
      ApplicationPath = "cmd.exe",
      Arguments = "/c echo test"
    };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    ValidateSuccessResult(result);
  }

  public async Task Exception_Given_InvalidApplicationPath()
  {
    // Arrange
    var command = new Command { ApplicationPath = "nonexistent.exe" };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsT2.ShouldBeTrue(); // Should be Exception
    Exception exception = result.AsT2;
    exception.Message.ShouldContain("An error occurred trying to start process");
  }

  public async Task ValidationResult_Given_EmptyApplicationPath()
  {
    // Arrange
    var command = new Command { ApplicationPath = string.Empty };
    var validator = new Validator();

    // Act
    ValidationResult validationResult = await validator.ValidateAsync(command);

    // Assert
    validationResult.IsValid.ShouldBeFalse();
    validationResult.Errors.Count.ShouldBe(1);
    validationResult.Errors[0].PropertyName.ShouldBe(nameof(Command.ApplicationPath));
  }

  public async Task ProcessId_Given_NullArguments()
  {
    // Arrange
    var command = new Command
    {
      ApplicationPath = "notepad.exe",
      Arguments = null
    };

    // Act
    OneOf<Response, ValidationResult, Exception> result = await Handler.Handle(command, CancellationToken.None);

    // Assert
    ValidateSuccessResult(result);
  }

  private void ValidateSuccessResult(OneOf<Response, ValidationResult, Exception> result)
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
