#region Purpose
// Marks a request as requiring an authenticated user and carries the caller's UserId.
#endregion

#region Design
// UserId duplicates the token's NameIdentifier claim by design: the server still validates the
// token as the source of truth, while the explicit property lets the MockAPI produce
// user-specific responses without any real authentication in play.
// The validator targets the interface, so one NotEmpty rule covers every auth request; concrete
// request validators Include it via mixins instead of restating it.
#endregion

namespace TimeWarp.Foundation.Features;

public interface IAuthApiRequest : IApiRequest
{
  /// <summary>
  /// The User Id of the current user.
  /// </summary>
  /// <remarks>This should match the NameIdentifier claim.
  /// The Server must always validate the token before trusting any claim.
  /// The UserId should equal the NameIdentifier is a secondary check.</remarks>
  /// <remarks>This facilitates The MockAPI to give better responses to exercise the UX.</remarks>
  public Guid UserId { get; set; }
}

public sealed class AuthApiRequestValidator : AbstractValidator<IAuthApiRequest>
{
  public AuthApiRequestValidator()
  {
    RuleFor(r => r.UserId).NotEmpty();
  }
}
