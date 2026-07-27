#region Purpose
// Unifies MediatR's IBaseRequest with TimeWarp.State's IAction so state actions can travel through the MediatR pipeline.
#endregion

namespace TimeWarp.Architecture.Features;

public interface IBaseAction : IBaseRequest, IAction;
