namespace TimeWarp.Blazor.Client.Features.EventStream
{
  using MediatR;

  public class AddEventAction : IRequest<EventStreamState>
  {
    public string Message { get; set; }
  }
}
