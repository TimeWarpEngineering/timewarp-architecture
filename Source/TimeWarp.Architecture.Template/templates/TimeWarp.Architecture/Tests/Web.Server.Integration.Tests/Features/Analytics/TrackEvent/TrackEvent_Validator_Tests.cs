namespace TrackEventRequestValidator_;

using static TimeWarp.Architecture.Features.Analytics.Contracts.TrackEvent;

public class Validate_Should
{
  private Validator Validator;

  public void Be_Valid()
  {
    var command = new Command
    {
      EventName = "SomeEvent"
    };

    ValidationResult validationResult = Validator.TestValidate(command);

    validationResult.IsValid.Should().BeTrue();
  }

  public void Have_error_when_EventName_is_empty()
  {
    TestValidationResult<Command> result =
      Validator.TestValidate(new Command { EventName = "" });

    result.ShouldHaveValidationErrorFor(aTrackEventRequest => aTrackEventRequest.EventName);
  }

  public void Setup() => Validator = new Validator();
}
