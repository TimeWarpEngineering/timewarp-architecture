#region Purpose
// Unifies Foundation IBaseRequest with TimeWarp.Mediator.IAction so state actions travel through the generated mediator pipeline.
#endregion

namespace TimeWarp.Architecture.Features;

public interface IBaseAction : IBaseRequest, IAction;
