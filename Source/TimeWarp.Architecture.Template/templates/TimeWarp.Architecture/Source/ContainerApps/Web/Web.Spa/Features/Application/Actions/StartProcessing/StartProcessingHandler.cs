﻿namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
  internal class StartProcessingHandler : BaseHandler<StartProcessingAction>
  {
    public StartProcessingHandler(IStore aStore) : base(aStore) { }

    public override Task Handle(StartProcessingAction aStartWaitingAction, CancellationToken aCancellationToken)
    {
      ApplicationState._ProcessingList.Add(aStartWaitingAction.ActionName);
      return Task.CompletedTask;
    }
  }
}
