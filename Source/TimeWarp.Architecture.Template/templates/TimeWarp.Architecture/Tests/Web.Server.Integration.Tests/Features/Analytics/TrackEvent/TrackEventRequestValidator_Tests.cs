namespace TrackEventRequestValidator_;

using FluentAssertions;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using TimeWarp.Architecture.Features.Analytics;

public class Validate_Should
{
  private TrackEventRequestValidator TrackEventRequestValidator;

  public void Be_Valid()
  {
    var trackEventRequest = new TrackEventRequest
    {
      EventName = "SomeEvent"
    };

    ValidationResult validationResult = TrackEventRequestValidator.TestValidate(trackEventRequest);

    validationResult.IsValid.Should().BeTrue();
  }

  public void Have_error_when_EventName_is_empty()
  {
    TestValidationResult<TrackEventRequest> result =
      TrackEventRequestValidator.TestValidate(new TrackEventRequest { EventName = "" });

    result.ShouldHaveValidationErrorFor(aTrackEventRequest => aTrackEventRequest.EventName);
  }

  public void Setup() => TrackEventRequestValidator = new TrackEventRequestValidator();
}
