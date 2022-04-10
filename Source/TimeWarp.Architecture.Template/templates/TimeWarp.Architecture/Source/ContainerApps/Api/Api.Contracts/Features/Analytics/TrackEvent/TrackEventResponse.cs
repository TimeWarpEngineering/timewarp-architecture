namespace TimeWarp.Architecture.Features.Analytics;

public record TrackEventResponse : BaseResponse
{
  public TrackEventResponse(Guid aCorrelationId) : base(aCorrelationId) { }
}
