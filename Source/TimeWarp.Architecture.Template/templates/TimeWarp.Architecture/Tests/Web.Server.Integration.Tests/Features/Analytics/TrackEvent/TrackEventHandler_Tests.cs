namespace TrackEventHandler_;

using FluentAssertions;
using System.Threading.Tasks;
using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Testing;

public class Handle_Returns
{
  private readonly TrackEventRequest TrackEventRequest;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Handle_Returns
  (
     WebTestServerApplication aWebTestServerApplication
  )
  {
    TrackEventRequest = new TrackEventRequest { EventName = "SomeEvent" };
    WebTestServerApplication = aWebTestServerApplication;
  }

  public async Task _Ok_Given_ValidRequest()
  {
    TrackEventResponse TrackEventResponse = await WebTestServerApplication.Send(TrackEventRequest);

    ValidateTrackEventResponse(TrackEventResponse);
  }

  private void ValidateTrackEventResponse(TrackEventResponse aTrackEventResponse)
  {
  }
}
