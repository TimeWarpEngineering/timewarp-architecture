#region Purpose
// Unifies TimeWarp.Mediator's IBaseRequest with TimeWarp.State's IAction so state actions can travel through the mediator pipeline.
#endregion

namespace TimeWarp.Architecture.Features;

public interface IBaseAction : IBaseRequest, IAction;
