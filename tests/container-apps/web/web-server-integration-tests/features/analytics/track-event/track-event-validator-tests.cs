namespace TrackEventRequestValidator_;

using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public class Validate_Should
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Validate_Should>();

  public static Task Be_Valid()

  {
    var command = new Command
    {
      EventName = "SomeEvent"
    };

    ValidationResult validationResult = new Validator().TestValidate(command);

    validationResult.IsValid.ShouldBeTrue();

    return Task.CompletedTask;

  }

  public static Task Have_error_when_EventName_is_empty()

  {
    TestValidationResult<Command> result =
      new Validator().TestValidate(new Command { EventName = "" });

    result.ShouldHaveValidationErrorFor(trackEventRequest => trackEventRequest.EventName);

    return Task.CompletedTask;

  }

}
