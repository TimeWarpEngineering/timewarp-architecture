#region Purpose
// Common base for domain events so they publish through the MediatR notification pipeline.
#endregion

namespace TimeWarp.Foundation.Entities.Base;

public abstract class BaseEvent : INotification { }
