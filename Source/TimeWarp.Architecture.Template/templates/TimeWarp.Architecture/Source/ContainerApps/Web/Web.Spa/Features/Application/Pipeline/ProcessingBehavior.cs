namespace TimeWarp.Architecture.Features.Applications;

using static TimeWarp.Architecture.Features.Applications.Spa.ApplicationState;

public class ProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  private readonly ISender Sender;

  public ProcessingBehavior(ISender sender) { Sender = sender; }

  public async Task<TResponse> Handle
  (
    TRequest aRequest,
    RequestHandlerDelegate<TResponse> aNextHandler,
    CancellationToken aCancellationToken
  )
  {
    if (typeof(TRequest).GetCustomAttributes(typeof(TrackProcessingAttribute), false).Any())
    {
      Guard.Support(aRequest is IAction);
      Guard.Argument(aRequest as object, nameof(aRequest))
        .NotType<StartProcessingAction>()
        .NotType<CompleteProcessingAction>();

      string actionName = typeof(TRequest).Name;
      await Sender.Send(new StartProcessingAction(actionName)).ConfigureAwait(false);
      TResponse response = await aNextHandler().ConfigureAwait(false);
      await Sender.Send(new CompleteProcessingAction(actionName)).ConfigureAwait(false);
      return response;
    }
    else
    {
      TResponse response = await aNextHandler().ConfigureAwait(false);
      return response;
    }
  }
}
