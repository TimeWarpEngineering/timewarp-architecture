namespace TrackEventRequestValidator_;

using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public class Validate_Should
{
  private Validator Validator = new();

  public void Be_Valid()
  {
    var command = new Command
    {
      EventName = "SomeEvent"
    };

    ValidationResult validationResult = Validator.TestValidate(command);

    validationResult.IsValid.ShouldBeTrue();
  }

  public void Have_error_when_EventName_is_empty()
  {
    TestValidationResult<Command> result =
      Validator.TestValidate(new Command { EventName = "" });

    result.ShouldHaveValidationErrorFor(trackEventRequest => trackEventRequest.EventName);
  }

  public void Setup() => Validator = new Validator();
}
