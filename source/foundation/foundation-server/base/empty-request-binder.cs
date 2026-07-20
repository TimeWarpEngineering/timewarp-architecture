#region Purpose
// FastEndpoints request binder for empty (propertyless) contract request DTOs.
#endregion

#region Design
// FE's default RequestBinder rejects DTOs with zero public properties at type-init time
// (NotSupportedException → 500). Identity ceremony "start" commands and empty GET queries are
// deliberately propertyless — the server mints all state. Returning new TRequest() preserves that
// contract shape without inventing dummy properties just for the binder.
#endregion

namespace TimeWarp.Foundation.Features;

public sealed class EmptyRequestBinder<TRequest> : IRequestBinder<TRequest>
  where TRequest : class, new()
{
  public ValueTask<TRequest> BindAsync(BinderContext context, CancellationToken cancellation)
    => ValueTask.FromResult(new TRequest());
}
