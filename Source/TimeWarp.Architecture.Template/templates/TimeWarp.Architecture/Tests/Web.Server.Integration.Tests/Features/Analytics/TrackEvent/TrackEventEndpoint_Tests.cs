namespace TrackEventEndpoint_;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Testing;

public class Returns
{
  private readonly TrackEventRequest TrackEventRequest;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns
  (
    WebTestServerApplication aWebTestServerApplication
  )
  {
    TrackEventRequest = new TrackEventRequest { EventName = "MyEvent" };
    WebTestServerApplication = aWebTestServerApplication;
  }

  public async Task _Ok_Given_SomeEvent()
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

  private void ValidateTrackEventResponse(TrackEventResponse aTrackEventResponse)
  {
  }
}
