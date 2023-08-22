namespace TimeWarp.Architecture.Features.Applications;

using static TimeWarp.Architecture.Features.Applications.ApplicationState;

public class ProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IAction
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
      Guard.Argument(aRequest as object, nameof(aRequest))
        .NotType<StartProcessing.Action>()
        .NotType<CompleteProcessing.Action>();

      string actionName = typeof(TRequest).Name;
      await Sender.Send(new StartProcessing.Action(actionName), aCancellationToken).ConfigureAwait(false);
      TResponse response = await aNextHandler().ConfigureAwait(false);
      await Sender.Send(new CompleteProcessing.Action(actionName), aCancellationToken).ConfigureAwait(false);
      return response;
    }
    else
    {
      TResponse response = await aNextHandler().ConfigureAwait(false);
      return response;
    }
  }
}
