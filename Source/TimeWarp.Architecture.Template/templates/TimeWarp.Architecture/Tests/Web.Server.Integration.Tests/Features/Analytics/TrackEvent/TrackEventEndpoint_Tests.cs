namespace TrackEventEndpoint_;

public class Returns_
{
  private readonly TrackEventRequest TrackEventRequest;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_
  (
    WebTestServerApplication aWebTestServerApplication
  )
  {
    TrackEventRequest = new TrackEventRequest { EventName = "MyEvent" };
    WebTestServerApplication = aWebTestServerApplication;
  }

  public async Task Ok_Given_SomeEvent()
  {
    TrackEventResponse trackEventResponse =
      await WebTestServerApplication.GetResponse<TrackEventResponse>(TrackEventRequest);

    ValidateTrackEventResponse(trackEventResponse);
  }

  public async Task ValidationError()
  {
    TrackEventRequest.EventName = "";

    await WebTestServerApplication.ConfirmEndpointValidationError<TrackEventResponse>(TrackEventRequest, nameof(TrackEventRequest.EventName));
  }

  private void ValidateTrackEventResponse(TrackEventResponse aTrackEventResponse) =>
    aTrackEventResponse.CorrelationId.Should().Be(TrackEventRequest.CorrelationId);
}
