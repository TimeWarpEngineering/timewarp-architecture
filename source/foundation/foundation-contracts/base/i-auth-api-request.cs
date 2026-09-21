#region Purpose
// Marks a request as requiring an authenticated user and carries the caller's UserId.
#endregion

#region Design
// UserId duplicates the token's NameIdentifier claim by design: the server still validates the
// token as the source of truth, while the explicit property lets the MockAPI produce
// user-specific responses without any real authentication in play.
// The validator targets the interface, so one NotEmpty rule covers every auth request; concrete
// request validators Include this validator instead of restating the rule.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// API request that carries the caller's <see cref="UserId"/> for authenticated operations.
/// </summary>
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

/// <summary>
/// Shared FluentValidation rules for every <see cref="IAuthApiRequest"/>.
/// </summary>
public sealed class AuthApiRequestValidator : AbstractValidator<IAuthApiRequest>
{
  /// <summary>
  /// Requires a non-empty <see cref="IAuthApiRequest.UserId"/>.
  /// </summary>
  public AuthApiRequestValidator()
  {
    RuleFor(r => r.UserId).NotEmpty();
  }
}
