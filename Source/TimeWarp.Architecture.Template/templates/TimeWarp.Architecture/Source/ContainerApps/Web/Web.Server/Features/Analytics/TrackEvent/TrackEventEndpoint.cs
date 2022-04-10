namespace TimeWarp.Architecture.Features.Analytics.TrackEvent;

using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

public class TrackEventEndpoint : BaseEndpoint<TrackEventRequest, TrackEventResponse>
{
  /// <summary>
  /// Track events in analytics
  /// </summary>
  /// <param name="aTrackEventRequest"><see cref="TrackEventRequest"/></param>
  /// <returns><see cref="TrackEventResponse"/></returns>
  [HttpPost(TrackEventRequest.Route)]
  [SwaggerOperation(Tags = new[] { FeatureAnnotations.FeatureGroup })]
  [ProducesResponseType(typeof(TrackEventResponse), (int)HttpStatusCode.OK)]
  [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
  public Task<IActionResult> Process([FromBody] TrackEventRequest aTrackEventRequest) => Send(aTrackEventRequest);
}
